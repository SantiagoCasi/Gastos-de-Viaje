namespace GastosDeViaje.Services;

/// <summary>
/// Genera el comprobante en PDF de una liquidación (RF13).
/// </summary>
public interface IComprobanteService
{
    /// <summary>Arma el PDF con el encabezado de la sesión, la tabla de gastos, el cuadro del cálculo y las transferencias.</summary>
    Task<byte[]> GenerarPdfAsync(int liquidacionId);
}
