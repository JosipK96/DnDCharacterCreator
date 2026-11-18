// The development worker caches nothing, so a code change shows up on reload. The published
// app uses service-worker.published.js instead (the project file's ServiceWorker item).
self.addEventListener('fetch', () => { });
