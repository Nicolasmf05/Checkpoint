// Service worker para los archivos estáticos de Checkpoint.
// Intenta obtener la versión de red y usa la copia en caché cuando no hay conexión.

const cacheName = 'checkpoint-web-1';
const assets = [
  './',
  'index.html',
  'app.html',
  'presentation.mjs',
  'presentation.css',
  'start.mjs',
  'bridge.mjs',
  'review.mjs',
  'model.mjs',
  'store.mjs',
  'api.mjs',
  'ui.js',
  'ui-model.mjs',
  'shortcuts.mjs',
  'app.css',
  'web.css',
  'config.json',
  'en.json',
  'icon.svg',
  'manifest.webmanifest',
];
self.addEventListener('install', (event) =>
  event.waitUntil(
    caches
      .open(cacheName)
      .then((cache) => cache.addAll(assets.map((url) => new Request(url, { cache: 'reload' }))))
      .then(() => self.skipWaiting()),
  ),
);
self.addEventListener('activate', (event) =>
  event.waitUntil(
    caches
      .keys()
      .then((keys) =>
        Promise.all(
          keys
            .filter((key) => key.startsWith('checkpoint-web-') && key !== cacheName)
            .map((key) => caches.delete(key)),
        ),
      )
      .then(() => self.clients.claim()),
  ),
);
self.addEventListener('fetch', (event) => {
  const url = new URL(event.request.url);
  if (
    event.request.method !== 'GET' ||
    url.origin !== self.location.origin ||
    !url.pathname.startsWith(new URL('./', self.location.href).pathname)
  )
    return;
  event.respondWith(
    fetch(new Request(event.request, { cache: 'no-cache' }))
      .then((response) => {
        if (
          response.ok &&
          assets.some((asset) => new URL(asset, self.location.href).pathname === url.pathname)
        ) {
          const copy = response.clone();
          caches.open(cacheName).then((cache) => cache.put(event.request, copy));
        }
        return response;
      })
      .catch(async () => {
        const cached = await caches.match(event.request);
        if (cached) return cached;
        // History-only language/theme/view changes do not create a new cached page.
        // Reuse only known documents; versioned assets must keep their exact URLs.
        const scope = new URL('./', self.location.href);
        const documents = [scope, new URL('index.html', scope), new URL('app.html', scope)];
        if (
          event.request.mode === 'navigate' &&
          url.search &&
          documents.some((document) => document.pathname === url.pathname)
        ) {
          const documentUrl = new URL(url.href);
          documentUrl.search = '';
          documentUrl.hash = '';
          const cache = await caches.open(cacheName);
          const document = await cache.match(documentUrl.href);
          if (document) return document;
        }
        throw new Error('offline');
      }),
  );
});
