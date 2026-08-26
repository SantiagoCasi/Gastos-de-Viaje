using GastosDeViaje.Models.Enums;

namespace GastosDeViaje.ViewModels;

/// <summary>
/// Detalle matemático de una liquidación (RF10): totales, la fila Pagado/Cuota/Saldo
/// de cada participante y la lista de transferencias sugeridas.
/// </summary>
public class DetalleLiquidacionViewModel
{
    public int LiquidacionId { get; set; }
    public int SesionViajeId { get; set; }
    public string SesionNombre { get; set; } = string.Empty;
    public string Moneda { get; set; } = string.Empty;
    public TipoLiquidacion Tipo { get; set; }
    public DateTime Fecha { get; set; }
    public decimal TotalGastado { get; set; }
    public int CantidadParticipantes { get; set; }
    public decimal CuotaIdeal { get; set; }
    public List<FilaParticipanteViewModel> Participantes { get; set; } = new();
    public List<TransferenciaViewModel> Transferencias { get; set; } = new();
}

/// <summary>Fila Pagado/Cuota/Saldo de un participante dentro del detalle de una liquidación.</summary>
public class FilaParticipanteViewModel
{
    public string Nombre { get; set; } = string.Empty;
    public decimal Pagado { get; set; }
    public decimal Cuota { get; set; }
    public decimal Saldo { get; set; }
}

/// <summary>Una transferencia sugerida: "Deudor le paga Monto a Acreedor".</summary>
public class TransferenciaViewModel
{
    public string DeudorNombre { get; set; } = string.Empty;
    public string AcreedorNombre { get; set; } = string.Empty;
    public decimal Monto { get; set; }
}
