using System.ComponentModel.DataAnnotations.Schema;
using GastosDeViaje.Models.Enums;

namespace GastosDeViaje.Models;

/// <summary>
/// Un corte de cuentas de una sesión de viaje: guarda la foto del cálculo (total,
/// cantidad de participantes, cuota ideal) y las transferencias que resultaron de él.
/// Existe para poder distinguir gastos ya saldados de pendientes y para reconstruir
/// el detalle matemático (RF10) y el comprobante (RF13) de cada corte.
/// </summary>
/// Cubre RF08, RF09, RF10, RNF06.
public class Liquidacion
{
    public int Id { get; set; }

    public int SesionViajeId { get; set; }

    public DateTime Fecha { get; set; }

    public TipoLiquidacion Tipo { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalGastado { get; set; }

    public int CantidadParticipantes { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CuotaIdeal { get; set; }

    public ICollection<Gasto> Gastos { get; set; } = new List<Gasto>();
    public ICollection<MovimientoLiquidacion> Movimientos { get; set; } = new List<MovimientoLiquidacion>();
}
