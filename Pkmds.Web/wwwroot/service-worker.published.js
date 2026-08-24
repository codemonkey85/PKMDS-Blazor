// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => {
    event.waitUntil((async () => {
        await onInstall(event);

        // Keep healthy updates waiting until existing pages close or explicitly apply them.
        // The one exception is a legacy cache that cannot boot after deployment because it did
        // not pre-cache its fingerprinted native runtime. Such a page can only ever serve the
        // old recovery code, while this complete worker remains waiting behind it. Activate the
        // complete worker so the user's next reload escapes that loop. Do not navigate/reload
        // clients here: an already-running editor may contain unsaved changes.
        if (await hasLegacyCacheWithoutNativeRuntime()) {
            console.warn('SW: Legacy cache is missing its native runtime; activating recovery worker.');
            const cache = await caches.open(cacheName);
            await cache.put(legacyRecoveryMarkerUrl, new Response('true'));
            await self.skipWaiting();
        }
    })());
});

self.addEventListener('activate', event => {
    event.waitUntil((async () => {
        const shouldClaimClients = await onActivate(event);
        // Explicit updates wait for controllerchange before reloading. Claiming here completes
        // that handoff; natural activation normally has no existing clients to claim.
        if (shouldClaimClients) {
            await self.clients.claim();
        }
    })());
});
self.addEventListener('message', event => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        event.waitUntil(self.skipWaiting());
    }
});
self.addEventListener('fetch', event => {
    const clientId = event.resultingClientId || event.clientId;
    const isNavigation = event.request.mode === 'navigate';
    if (clientId && (!trackedClientIds.has(clientId) || isNavigation)) {
        trackedClientIds.add(clientId);
        event.waitUntil(recordClientCacheLease(clientId)
            .then(async () => {
                if (isNavigation) {
                    await pruneUnusedAppCaches();
                }
            })
            .catch(err => {
                trackedClientIds.delete(clientId);
                console.warn('SW: Failed to record client cache lease or prune unused caches.', err);
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
// appsettings*.json is intentionally NOT excluded: our appsettings.json is
// static (just the Azure function URL) and excluding it caused 429 failures
// for users on iCloud Private Relay / GitHub Pages rate-limited IPs, crashing
// the Blazor bootstrap before the app could start (issue #910).
//
// dotnet.native.*.wasm used to be excluded when AOT made it roughly 44 MB.
// AOT is disabled now and the runtime is roughly 3 MB. It must be cached: an
// old app version cannot boot after a deploy removes its fingerprinted runtime
// from the server, which caused the recurring 98% startup failures (#1142+).
// Bundled sprites are cached on demand below. Pre-caching thousands of them can
// trigger transient GitHub Pages 503 responses and reject the whole worker install.
const offlineAssetsExclude = [/^service-worker\.js$/, /^staticwebapp\.config\.json$/, /^_content\/Pkmds\.Rcl\/sprites\//];

// Replace with your base path if you are hosting on a subfolder. Ensure there is a trailing '/'.
const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);
const bundledSpritePathPrefix = new URL('_content/Pkmds.Rcl/sprites/', baseUrl).href;
const nativeRuntimePath = /\/_framework\/dotnet\.native\.[^/]+\.wasm$/;
const legacyRecoveryMarkerUrl = new URL('__legacy-cache-recovery__', baseUrl).href;

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

async function hasLegacyCacheWithoutNativeRuntime() {
    // A stale cache by itself must not bypass the normal waiting-worker handoff. Workers from
    // before cache leases can only strand a client when such an unleased client is still live.
    const {hasUnleasedClients} = await getLiveClientCacheLeases();
    if (!hasUnleasedClients) {
        return false;
    }

    const cacheKeys = await caches.keys();
    const legacyCacheKeys = cacheKeys.filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName);

    for (const legacyCacheKey of legacyCacheKeys) {
        const legacyCache = await caches.open(legacyCacheKey);
        const cachedRequests = await legacyCache.keys();
        if (!cachedRequests.some(request => nativeRuntimePath.test(new URL(request.url).pathname))) {
            return true;
        }
    }

    return false;
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    const currentCache = await caches.open(cacheName);
    const isLegacyRecovery = Boolean(await currentCache.match(legacyRecoveryMarkerUrl));
    if (isLegacyRecovery) {
        // Do not claim or prune a successfully running legacy tab. Its next deliberate reload
        // will use this now-active worker, while its current editor and cache remain untouched.
        console.warn('SW: Preserving legacy clients and cache until their next navigation.');
        await currentCache.delete(legacyRecoveryMarkerUrl);
        return false;
    }

    await pruneUnusedAppCaches();
    return true;
}

async function pruneUnusedAppCaches() {
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
    // Cache bundled sprites lazily in the current version cache instead of requesting thousands
    // during worker installation. PokeAPI sprites use a separate cache that survives app updates.
    // Return the promise rather than calling event.respondWith() here: the outer 'fetch' listener
    // already wraps onFetch(event) in respondWith, and double-calling it throws InvalidStateError
    // (and on iOS Safari surfaces as `FetchEvent.respondWith received an error: TypeError: ...`).
    const isPokeApiSprite = event.request.url.startsWith(spriteOrigin);
    const isBundledSprite = event.request.url.startsWith(bundledSpritePathPrefix);
    if (event.request.method === 'GET' && (isPokeApiSprite || isBundledSprite)) {
        const cache = await caches.open(isPokeApiSprite ? spriteCacheName : cacheName);
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
