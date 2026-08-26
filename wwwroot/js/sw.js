// Service worker de la PWA (RNF01, RNF02, sección 6 del prompt maestro).
// Estrategia cache-first para el shell (CSS, JS, iconos, manifest): casi no cambia,
// así que se sirve de entrada desde el cache y se refresca en segundo plano.
// Estrategia network-first para todo lo demás (páginas MVC): si hay red se usa la
// versión más nueva; si no hay red, se cae a lo último que quedó cacheado.
var NOMBRE_CACHE = 'gastos-de-viaje-v1';

var ARCHIVOS_DEL_SHELL = [
    '/css/site.css',
    '/js/site.js',
    '/js/app.js',
    '/js/offline.js',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/lib/jquery/dist/jquery.min.js',
    '/icons/icon-192.png',
    '/icons/icon-512.png',
    '/manifest.json'
];

self.addEventListener('install', function (evento) {
    evento.waitUntil(
        caches.open(NOMBRE_CACHE).then(function (cache) {
            return cache.addAll(ARCHIVOS_DEL_SHELL);
        })
    );
    self.skipWaiting();
});

self.addEventListener('activate', function (evento) {
    // Borra caches de versiones anteriores del service worker.
    evento.waitUntil(
        caches.keys().then(function (nombres) {
            return Promise.all(
                nombres
                    .filter(function (nombre) { return nombre !== NOMBRE_CACHE; })
                    .map(function (nombre) { return caches.delete(nombre); })
            );
        })
    );
    self.clients.claim();
});

self.addEventListener('fetch', function (evento) {
    if (evento.request.method !== 'GET') {
        return; // los POST (formularios, sync) siempre van directo a la red.
    }

    var url = new URL(evento.request.url);
    var esArchivoDelShell = ARCHIVOS_DEL_SHELL.indexOf(url.pathname) !== -1;

    if (esArchivoDelShell) {
        evento.respondWith(
            caches.match(evento.request).then(function (respuestaCacheada) {
                var actualizarEnSegundoPlano = fetch(evento.request)
                    .then(function (respuestaDeRed) {
                        caches.open(NOMBRE_CACHE).then(function (cache) {
                            cache.put(evento.request, respuestaDeRed.clone());
                        });
                        return respuestaDeRed;
                    })
                    .catch(function () { return respuestaCacheada; });

                return respuestaCacheada || actualizarEnSegundoPlano;
            })
        );
        return;
    }

    evento.respondWith(
        fetch(evento.request)
            .then(function (respuestaDeRed) {
                var copia = respuestaDeRed.clone();
                caches.open(NOMBRE_CACHE).then(function (cache) { cache.put(evento.request, copia); });
                return respuestaDeRed;
            })
            .catch(function () { return caches.match(evento.request); })
    );
});
