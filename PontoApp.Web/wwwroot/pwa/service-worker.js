// wwwroot/pwa/service-worker.js
const CACHE_VERSION = 'pontoapp-v3';            // << aumente quando mudar layout/assets
const STATIC_ASSETS = [
    '/manifest.webmanifest',
    '/img/icon-192.png',
    '/img/icon-512.png'
];

// instala e pré-cacheia apenas assets estáticos
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_VERSION).then(cache => cache.addAll(STATIC_ASSETS))
    );
    self.skipWaiting();
});

// limpa caches antigos quando um novo SW ativa
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(keys.filter(k => k !== CACHE_VERSION).map(k => caches.delete(k)))
        )
    );
    self.clients.claim();
});

// Estratégia:
// - Para navegação (HTML): network-first (evita HTML antigo do cache)
// - Para estáticos: cache-first
self.addEventListener('fetch', event => {
    const req = event.request;

    // páginas HTML / navegação
    if (req.mode === 'navigate' || (req.headers.get('accept') || '').includes('text/html')) {
        event.respondWith(
            fetch(req).catch(() => caches.match(req).then(r => r || caches.match('/')))
        );
        return;
    }

    // assets estáticos (imagens, css, js)
    event.respondWith(
        caches.match(req).then(cached =>
            cached ||
            fetch(req).then(res => {
                const resClone = res.clone();
                caches.open(CACHE_VERSION).then(cache => cache.put(req, resClone));
                return res;
            })
        )
    );
});
