namespace GastosDeViaje.Models.Enums;

/// <summary>
/// Estado de una <see cref="SesionViaje"/>. Una vez <see cref="Cerrada"/> no admite
/// nuevos gastos (RF09).
/// </summary>
public enum EstadoSesion
{
    Abierta,
    Cerrada
}



/*
enum (enumeración) es un tipo de dato que define un conjunto fijo y cerrado de valores con nombre. En este archivo, EstadoSesion solo puede valer Abierta o Cerrada — ningún otro valor es válido, el compilador lo garantiza.
Por dentro, cada valor es en realidad un número entero (Abierta = 0, Cerrada = 1 por defecto), pero en el código nunca escribís esos números — usás el nombre, que es legible.
public es el modificador de acceso: significa que esta clase/enum puede usarse desde cualquier otro archivo o proyecto que referencie a GastosDeViaje. Sin public, por defecto solo sería visible dentro del mismo namespace/ensamblado.


¿Por qué usar enum acá en vez de, por ejemplo, un string o bool?
Evita "magic strings": sin el enum, alguien podría escribir sesion.Estado = "abierta" en un lugar y "Abierta" (con mayúscula) en otro, o directamente "abierto" mal tipeado. Con el enum, SesionViaje.Estado = EstadoSesion.Abierta — el compilador rechaza cualquier valor que no exista.
Autocompletado e intención clara: al escribir EstadoSesion. Visual Studio te muestra las únicas dos opciones posibles.
Más expresivo que un bool: podrías haber usado bool EstaCerrada, pero si mañana aparece un tercer estado (por ejemplo Archivada), un bool no escala y un enum sí — solo agregás un valor nuevo.
Se traduce directo a una restricción en la base de datos: Entity Framework guarda el enum como int (o string, según configuración), y el hecho de que solo existan esos dos valores queda modelado tanto en el código como en los datos.
En este caso puntual, el comentario del archivo aclara la regla de negocio: una vez que SesionViaje está Cerrada, no se pueden agregar más gastos (RF09) — el enum es lo que le permite al código preguntar if (sesion.Estado == EstadoSesion.Cerrada) de forma clara en vez de comparar strings o números sueltos.

*/