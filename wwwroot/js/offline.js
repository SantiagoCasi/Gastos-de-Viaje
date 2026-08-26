// Cola de gastos cargados sin conexión (RF06, RNF01, RNF03). Si el formulario de
// "Cargar gasto" se manda sin red, el gasto se guarda en IndexedDB en vez de perderse;
// el banner de esta página muestra cuántos quedan sin sincronizar, y apenas vuelve la
// conexión se mandan todos juntos a POST /api/sync/gastos.
var NOMBRE_BASE_OFFLINE = 'GastosDeViajeOffline';
var NOMBRE_TABLA_COLA = 'colaGastos';

// Abre (o crea, la primera vez) la base IndexedDB con la tabla de la cola.
function abrirBaseOffline() {
    return new Promise(function (resolve, reject) {
        var solicitud = indexedDB.open(NOMBRE_BASE_OFFLINE, 1);

        solicitud.onupgradeneeded = function (evento) {
            var base = evento.target.result;
            if (!base.objectStoreNames.contains(NOMBRE_TABLA_COLA)) {
                base.createObjectStore(NOMBRE_TABLA_COLA, { keyPath: 'idTemporal' });
            }
        };
        solicitud.onsuccess = function (evento) { resolve(evento.target.result); };
        solicitud.onerror = function (evento) { reject(evento.target.error); };
    });
}

function agregarGastoALaCola(gasto) {
    return abrirBaseOffline().then(function (base) {
        return new Promise(function (resolve, reject) {
            var transaccion = base.transaction(NOMBRE_TABLA_COLA, 'readwrite');
            transaccion.objectStore(NOMBRE_TABLA_COLA).put(gasto);
            transaccion.oncomplete = function () { resolve(); };
            transaccion.onerror = function (evento) { reject(evento.target.error); };
        });
    });
}

function obtenerColaCompleta() {
    return abrirBaseOffline().then(function (base) {
        return new Promise(function (resolve, reject) {
            var solicitud = base.transaction(NOMBRE_TABLA_COLA, 'readonly')
                .objectStore(NOMBRE_TABLA_COLA)
                .getAll();
            solicitud.onsuccess = function () { resolve(solicitud.result); };
            solicitud.onerror = function (evento) { reject(evento.target.error); };
        });
    });
}

function quitarDeLaCola(idsTemporales) {
    return abrirBaseOffline().then(function (base) {
        return new Promise(function (resolve, reject) {
            var transaccion = base.transaction(NOMBRE_TABLA_COLA, 'readwrite');
            var tabla = transaccion.objectStore(NOMBRE_TABLA_COLA);
            idsTemporales.forEach(function (id) { tabla.delete(id); });
            transaccion.oncomplete = function () { resolve(); };
            transaccion.onerror = function (evento) { reject(evento.target.error); };
        });
    });
}

// Banner permanente: "Tenés N gastos sin sincronizar" (RNF01). Vive en _Layout.cshtml
// para verse en cualquier pantalla, no solo en la de carga de gastos.
function actualizarBannerPendientes() {
    var banner = document.getElementById('banner-pendientes');
    if (!banner) {
        return;
    }

    obtenerColaCompleta().then(function (cola) {
        if (cola.length === 0) {
            banner.classList.add('d-none');
            banner.textContent = '';
            return;
        }

        banner.classList.remove('d-none');
        banner.textContent = cola.length === 1
            ? 'Tenés 1 gasto sin sincronizar.'
            : 'Tenés ' + cola.length + ' gastos sin sincronizar.';
    });
}

// Al recuperar la conexión, manda toda la cola junta y borra de IndexedDB solo lo que
// el servidor confirmó (si algo falla, queda para el próximo intento).
function sincronizarConElServidor() {
    obtenerColaCompleta().then(function (cola) {
        if (cola.length === 0) {
            return;
        }

        fetch('/api/sync/gastos', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(cola)
        })
            .then(function (respuesta) { return respuesta.json(); })
            .then(function (confirmados) {
                var idsConfirmados = confirmados.map(function (c) { return c.idTemporal; });
                return quitarDeLaCola(idsConfirmados);
            })
            .then(actualizarBannerPendientes)
            .catch(function (error) {
                console.error('No se pudo sincronizar la cola de gastos offline.', error);
            });
    });
}

// Engancha el formulario de "Cargar gasto" (Views/Gasto/Create.cshtml): si no hay
// conexión, en vez de dejar que el POST normal falle, guarda el gasto en la cola.
function registrarEnvioOfflineDelFormularioDeGasto() {
    var formulario = document.getElementById('form-gasto');
    if (!formulario) {
        return;
    }

    formulario.addEventListener('submit', function (evento) {
        if (navigator.onLine) {
            return; // hay red: se manda como un POST de MVC normal.
        }

        if (!formulario.checkValidity()) {
            return; // deja que el navegador muestre los errores de validación nativos.
        }

        evento.preventDefault();

        var datos = new FormData(formulario);
        var sesionViajeId = datos.get('SesionViajeId');
        var gasto = {
            idTemporal: 'local-' + Date.now() + '-' + Math.random().toString(16).slice(2),
            sesionViajeId: parseInt(sesionViajeId, 10),
            participanteId: parseInt(datos.get('ParticipanteId'), 10),
            monto: parseFloat(datos.get('Monto')),
            fecha: datos.get('Fecha'),
            lugar: datos.get('Lugar'),
            motivo: datos.get('Motivo'),
            metodoPago: datos.get('MetodoPago')
        };

        agregarGastoALaCola(gasto).then(function () {
            actualizarBannerPendientes();
            alert('No hay conexión: el gasto se guardó en este dispositivo y se va a sincronizar solo apenas vuelva la señal.');
            window.location.href = '/Gasto?sesionViajeId=' + sesionViajeId;
        });
    });
}

document.addEventListener('DOMContentLoaded', function () {
    if (!('indexedDB' in window)) {
        return; // navegador sin IndexedDB: se pierde el soporte offline, la app sigue andando online.
    }

    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.register('/js/sw.js').catch(function (error) {
            console.error('No se pudo registrar el service worker.', error);
        });
    }

    actualizarBannerPendientes();
    registrarEnvioOfflineDelFormularioDeGasto();

    window.addEventListener('online', sincronizarConElServidor);
});
