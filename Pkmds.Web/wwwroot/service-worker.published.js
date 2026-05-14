// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => {
    self.skipWaiting();
    event.waitUntil(onInstall(event));
});

self.addEventListener('activate', event => {
    event.waitUntil(onActivate(event));
    self.clients.claim();
});
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const spriteCacheName = 'pokeapi-sprites-v1';
const spriteOrigin = 'https://raw.githubusercontent.com';
const CACHE_VERSION = '%%CACHE_VERSION%%'
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}${CACHE_VERSION}`;

const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.woff2$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.svg$/, /\.webp$/, /\.blat$/, /\.dat$/];
// AOT-compiled native blob (~44 MB raw, ~7.75 MB brotli) is excluded from
// SW pre-cache because it alone has caused iOS Safari to reject the entire
// install on tight per-origin SW cache quotas, breaking offline reload.
// Browsers continue to serve it via the regular HTTP cache (1-year max-age
// on the fingerprinted asset), so returning users with a populated HTTP
// cache still get offline reload; only stone-cold first-load-offline fails
// for this single file, which has always been a thin scenario.
const offlineAssetsExclude = [/^service-worker\.js$/, /^appsettings.*\.json$/, /^staticwebapp\.config\.json$/, /^_framework\/dotnet\.native\.[^\/]+\.wasm$/];

// Replace with your base path if you are hosting on a subfolder. Ensure there is a trailing '/'.
const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall(event) {
    console.info('Service worker: Install');

    // Fetch and cache all matching items from the assets manifest.
    // Use per-file error handling instead of cache.addAll() (which is all-or-nothing) so that a
    // single SRI hash mismatch — e.g. during the brief CDN propagation window after a new deploy
    // reaches GitHub Pages edge nodes at different times — does not abort the entire install.
    // Any file that fails to pre-cache here will simply be fetched live from the network until
    // the next successful install picks it up.
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, {integrity: asset.hash, cache: 'no-cache'}));
    const cache = await caches.open(cacheName);
    await Promise.allSettled(
        assetsRequests.map(req =>
            cache.add(req).catch(err => console.warn(`SW: Failed to pre-cache ${req.url}:`, err))
        )
    );
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Delete unused caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    // Cache-first strategy for PokeAPI sprites — separate long-lived cache that survives app updates
    if (event.request.method === 'GET' && event.request.url.startsWith(spriteOrigin)) {
        event.respondWith(
            caches.open(spriteCacheName).then(async cache => {
                const cached = await cache.match(event.request);
                if (cached) return cached;
                const response = await fetch(event.request);
                if (response.ok) cache.put(event.request, response.clone());
                return response;
            })
        );
        return;
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

    return cachedResponse || fetch(event.request);
}
