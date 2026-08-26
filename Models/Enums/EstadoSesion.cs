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
