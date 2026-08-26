using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GastosDeViaje.Models.Enums;

namespace GastosDeViaje.Models;

/// <summary>
/// Un gasto individual dentro de una sesión de viaje, pagado por un participante y
/// dividido en partes iguales entre todos (ver <c>BalanceService</c>). Mientras
/// <see cref="LiquidacionId"/> sea <c>null</c> el gasto está pendiente de saldar.
/// </summary>
/// Cubre RF05, RF06, RF07.
public class Gasto
{
    public int Id { get; set; }

    [Required]
    public int SesionViajeId { get; set; }

    /// <summary>Participante que pagó el gasto.</summary>
    [Required]
    public int ParticipanteId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Monto { get; set; }

    [Required]
    public DateTime Fecha { get; set; }

    [Required]
    [StringLength(120)]
    public string Lugar { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Motivo { get; set; } = string.Empty;

    public MetodoPago MetodoPago { get; set; }

    /// <summary>Liquidación que saldó este gasto. <c>null</c> = todavía pendiente.</summary>
    public int? LiquidacionId { get; set; }

    /// <summary>Se deriva de <see cref="LiquidacionId"/>: no se persiste un estado aparte.</summary>
    [NotMapped]
    public bool Saldado => LiquidacionId != null;
}
