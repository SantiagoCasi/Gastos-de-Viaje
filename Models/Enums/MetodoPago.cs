namespace GastosDeViaje.Models.Enums;

/// <summary>
/// Medio de pago utilizado para un <see cref="Gasto"/>. Cubre RF05.
/// </summary>
public enum MetodoPago
{
    Efectivo,
    Debito,
    Credito,
    Transferencia,
    Otro
}
