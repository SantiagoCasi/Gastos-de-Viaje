// Comparte el comprobante en PDF de una liquidación usando la Web Share API (RF14).
// Sin esto, en el celular no habría forma de "adjuntar" el PDF a un chat: hay que
// bajarlo como archivo primero y pasárselo al sistema operativo con navigator.share().
document.addEventListener('DOMContentLoaded', function () {
    var botonCompartir = document.querySelector('[data-compartir-pdf]');
    if (!botonCompartir) {
        return;
    }

    // Si el navegador no soporta compartir archivos, se oculta el botón: queda la
    // descarga directa y el enlace de WhatsApp como alternativa (ver Detalle.cshtml).
    if (!navigator.canShare || !navigator.share) {
        botonCompartir.classList.add('d-none');
        return;
    }

    botonCompartir.addEventListener('click', function () {
        var url = botonCompartir.getAttribute('data-comprobante-url');
        var nombreSesion = botonCompartir.getAttribute('data-sesion-nombre');

        fetch(url)
            .then(function (respuesta) {
                return respuesta.blob();
            })
            .then(function (blob) {
                var archivo = new File([blob], 'comprobante.pdf', { type: 'application/pdf' });

                if (!navigator.canShare({ files: [archivo] })) {
                    alert('Este dispositivo no admite compartir archivos PDF.');
                    return;
                }

                return navigator.share({
                    files: [archivo],
                    title: 'Comprobante de ' + nombreSesion,
                    text: 'Comprobante de ' + nombreSesion
                });
            })
            .catch(function (error) {
                // El usuario canceló el share o hubo un error de red: no hace falta avisar.
                console.error('No se pudo compartir el comprobante', error);
            });
    });
});
