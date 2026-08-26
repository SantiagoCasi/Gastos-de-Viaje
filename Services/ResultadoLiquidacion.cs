namespace GastosDeViaje.Services;

/// <summary>
/// Detalle matemático de una liquidación ya calculada (RF10): totales, la fila
/// Pagado/Cuota/Saldo de cada participante y las transferencias sugeridas. Lo arma
/// <see cref="IBalanceService.ObtenerDetalleAsync"/> a partir de datos ya persistidos,
/// y lo consumen tanto la vista de detalle como <see cref="IComprobanteService"/>, para
/// no duplicar la consulta en dos lugares.
/// </summary>
public class ResultadoLiquidacion
{
    public int LiquidacionId { get; set; }
    public int SesionViajeId { get; set; }
    public string SesionNombre { get; set; } = string.Empty;
    public string Moneda { get; set; } = string.Empty;
    public Models.Enums.TipoLiquidacion Tipo { get; set; }
    public DateTime Fecha { get; set; }
    public decimal TotalGastado { get; set; }
    public int CantidadParticipantes { get; set; }
    public decimal CuotaIdeal { get; set; }
    public List<BalanceParticipante> Participantes { get; set; } = new();
    public List<Transferencia> Transferencias { get; set; } = new();
    public List<GastoIncluido> Gastos { get; set; } = new();
}

/// <summary>Un gasto incluido en la liquidación, para la tabla de detalle del comprobante (RF13).</summary>
public class GastoIncluido
{
    public DateTime Fecha { get; set; }
    public string Lugar { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string ParticipanteNombre { get; set; } = string.Empty;
    public decimal Monto { get; set; }
}

/// <summary>Fila Pagado/Cuota/Saldo de un participante dentro de una liquidación.</summary>
public class BalanceParticipante
{
    public string Nombre { get; set; } = string.Empty;
    public decimal Pagado { get; set; }
    public decimal Cuota { get; set; }
    public decimal Saldo { get; set; }
}

/// <summary>Una transferencia sugerida: "Deudor le paga Monto a Acreedor".</summary>
public class Transferencia
{
    public string DeudorNombre { get; set; } = string.Empty;
    public string AcreedorNombre { get; set; } = string.Empty;
    public decimal Monto { get; set; }
}
