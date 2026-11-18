// Caches the published app's static files so a later launch starts from the cache. Built on
// Blazor's PWA template; service-worker-assets.js (generated at publish) lists the files and
// their hashes, and its version changes with any file, which is what makes a release an update.
self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall()));
self.addEventListener('activate', event => event.waitUntil(onActivate()));
self.addEventListener('fetch', event => {
    const response = onFetch(event);
    if (response) {
        event.respondWith(response);
    }
});
// The open app asks for the new worker to take over once the player accepts the update prompt
self.addEventListener('message', event => {
    if (event.data === 'SKIP_WAITING') {
        self.skipWaiting();
    }
});

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff2?$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.webmanifest$/];
const offlineAssetsExclude = [/^service-worker\.js$/];

// Deliberately no skipWaiting here: a new worker waits until the player accepts the prompt, so a
// client never changes under someone mid-wizard
async function onInstall() {
    const requests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(requests));
}

async function onActivate() {
    const keys = await caches.keys();
    await Promise.all(keys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

// Returns undefined to leave a request to the network untouched: anything that is not a GET, from
// another origin, or under api/ (never cached, so sign-in and every character call is live)
function onFetch(event) {
    const request = event.request;
    const url = new URL(request.url);
    if (request.method !== 'GET' || url.origin !== self.location.origin || url.pathname.startsWith('/api/')) {
        return undefined;
    }

    // A page navigation gets the app shell, as the Api's fallback does
    const wanted = request.mode === 'navigate' ? 'index.html' : request;
    return caches.open(cacheName)
        .then(cache => cache.match(wanted))
        .then(cached => cached || fetch(request));
}
