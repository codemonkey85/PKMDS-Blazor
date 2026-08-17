// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => {
    // Activate the downloaded worker promptly, but do not claim pages that are already
    // running the previous app version. They keep their matching worker/cache until reload.
    self.skipWaiting();
    event.waitUntil(onInstall(event));
});

self.addEventListener('activate', event => {
    event.waitUntil(onActivate(event));
});
self.addEventListener('message', event => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        event.waitUntil(self.skipWaiting());
    }
});
self.addEventListener('fetch', event => {
    const clientId = event.resultingClientId || event.clientId;
    if (clientId && !trackedClientIds.has(clientId)) {
        trackedClientIds.add(clientId);
        event.waitUntil(recordClientCacheLease(clientId).catch(err => {
            trackedClientIds.delete(clientId);
            console.warn('SW: Failed to record client cache lease.', err);
        }));
    }
    event.respondWith(onFetch(event));
});

const cacheNamePrefix = 'offline-cache-';
const cacheLeaseName = 'pkmds-client-cache-leases-v1';
const cacheLeasePathPrefix = '/__pkmds-client-cache-lease__/';
const spriteCacheName = 'pokeapi-sprites-v1';
const spriteOrigin = 'https://raw.githubusercontent.com';
const CACHE_VERSION = '%%CACHE_VERSION%%'
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}${CACHE_VERSION}`;
const trackedClientIds = new Set();

const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.woff2$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.svg$/, /\.webp$/, /\.blat$/, /\.dat$/];
// AOT-compiled native blob (~44 MB raw, ~7.75 MB brotli) is excluded from
// SW pre-cache because it alone has caused iOS Safari to reject the entire
// install on tight per-origin SW cache quotas, breaking offline reload.
// Browsers continue to serve it via the regular HTTP cache (1-year max-age
// on the fingerprinted asset), so returning users with a populated HTTP
// cache still get offline reload; only stone-cold first-load-offline fails
// for this single file, which has always been a thin scenario.
//
// appsettings*.json is intentionally NOT excluded: our appsettings.json is
// static (just the Azure function URL) and excluding it caused 429 failures
// for users on iCloud Private Relay / GitHub Pages rate-limited IPs, crashing
// the Blazor bootstrap before the app could start (issue #910).
const offlineAssetsExclude = [/^service-worker\.js$/, /^staticwebapp\.config\.json$/, /^_framework\/dotnet\.native\.[^\/]+\.wasm$/];

// Replace with your base path if you are hosting on a subfolder. Ensure there is a trailing '/'.
const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall(event) {
    console.info('Service worker: Install');

    // A worker must not install with a partial version cache. That creates a mixed app where
    // some boot resources are served from this deployment and missing ones fall through to the
    // network (or a newer deployment). Reject the install instead; the previous worker/cache
    // stays healthy and the browser can retry once the deployment has propagated.
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, {integrity: asset.hash, cache: 'no-cache'}));
    const cache = await caches.open(cacheName);
    try {
        await Promise.all(assetsRequests.map(req => cache.add(req)));
    } catch (err) {
        await caches.delete(cacheName);
        console.error('SW: Version cache install failed; keeping the previous worker active.', err);
        throw err;
    }
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // A tab can remain controlled by any older worker across multiple deployments. Each worker
    // records which version cache its clients use, so retain every cache leased by a live tab
    // instead of guessing that only the immediately previous cache can still be needed.
    const cacheKeys = await caches.keys();
    const {leasedCacheNames, hasUnleasedClients} = await getLiveClientCacheLeases();
    if (hasUnleasedClients) {
        // Older deployed workers do not know how to create leases, and a first-load page may be
        // uncontrolled. Preserve all app caches until a later activation can identify every
        // live client's cache rather than risking an in-use version during migration.
        console.info('SW: Live client without a cache lease; preserving prior app caches.');
        return;
    }

    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix)
            && !leasedCacheNames.has(key))
        .map(key => caches.delete(key)));
}

async function recordClientCacheLease(clientId) {
    const leaseCache = await caches.open(cacheLeaseName);
    await leaseCache.put(
        getClientCacheLeaseRequest(clientId),
        new Response(cacheName, {headers: {'Content-Type': 'text/plain'}}));
}

async function getLiveClientCacheLeases() {
    const liveClients = await self.clients.matchAll({type: 'window', includeUncontrolled: true});
    const liveClientIds = new Set(liveClients.map(client => client.id));
    const leasedClientIds = new Set();
    const leasedCacheNames = new Set([cacheName]);
    const leaseCache = await caches.open(cacheLeaseName);
    const leaseRequests = await leaseCache.keys();

    await Promise.all(leaseRequests.map(async request => {
        const clientId = getClientIdFromLeaseRequest(request);
        if (!clientId || !liveClientIds.has(clientId)) {
            await leaseCache.delete(request);
            return;
        }

        const response = await leaseCache.match(request);
        const leasedCacheName = response && await response.text();
        if (leasedCacheName && leasedCacheName.startsWith(cacheNamePrefix)) {
            leasedClientIds.add(clientId);
            leasedCacheNames.add(leasedCacheName);
        }
    }));

    return {
        leasedCacheNames,
        hasUnleasedClients: liveClients.some(client => !leasedClientIds.has(client.id)),
    };
}

function getClientCacheLeaseRequest(clientId) {
    const url = new URL(`${cacheLeasePathPrefix}${encodeURIComponent(clientId)}`, self.origin);
    return new Request(url.href);
}

function getClientIdFromLeaseRequest(request) {
    const pathname = new URL(request.url).pathname;
    if (!pathname.startsWith(cacheLeasePathPrefix)) return null;
    return decodeURIComponent(pathname.slice(cacheLeasePathPrefix.length));
}

async function onFetch(event) {
    // Cache-first strategy for PokeAPI sprites — separate long-lived cache that survives app updates.
    // Return the promise rather than calling event.respondWith() here: the outer 'fetch' listener
    // already wraps onFetch(event) in respondWith, and double-calling it throws InvalidStateError
    // (and on iOS Safari surfaces as `FetchEvent.respondWith received an error: TypeError: ...`).
    if (event.request.method === 'GET' && event.request.url.startsWith(spriteOrigin)) {
        const cache = await caches.open(spriteCacheName);
        const cached = await cache.match(event.request);
        if (cached) return cached;
        try {
            const response = await fetch(event.request);
            if (response.ok) cache.put(event.request, response.clone());
            return response;
        } catch {
            // Sprite fetch failed offline — return a synthetic empty response so respondWith
            // resolves cleanly. Callers tolerate a missing sprite image.
            return new Response('', {status: 504, statusText: 'Sprite fetch failed (offline)'});
        }
    }

    let cachedResponse = null;
    if (event.request.method === 'GET') {
        // For all navigation requests, try to serve index.html from cache,
        // unless that request is for an offline resource.
        // If you need some URLs to be server-rendered, edit the following check to exclude those URLs
        const shouldServeIndexHtml = event.request.mode === 'navigate'
            && !manifestUrlList.some(url => url === event.request.url);

        const request = shouldServeIndexHtml ? 'index.html' : event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }

    if (cachedResponse) return cachedResponse;

    // Network fallback — catch the rejection so respondWith doesn't surface
    // `TypeError: Load failed` on iOS Safari when the request fails mid-boot.
    // For navigations, fall back to cached index.html so the SPA shell still loads offline.
    try {
        return await fetch(event.request);
    } catch {
        if (event.request.mode === 'navigate') {
            const cache = await caches.open(cacheName);
            const shellResponse = await cache.match('index.html');
            if (shellResponse) return shellResponse;
        }
        return new Response('', {status: 504, statusText: 'Network unavailable'});
    }
}
