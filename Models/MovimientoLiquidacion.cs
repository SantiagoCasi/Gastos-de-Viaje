using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GastosDeViaje.Models;

/// <summary>
/// Una transferencia concreta resultante de una <see cref="Liquidacion"/>: el
/// <see cref="DeudorId"/> le paga <see cref="Monto"/> al <see cref="AcreedorId"/>.
/// </summary>
/// Cubre RF08, RF09, RF10.
public class MovimientoLiquidacion
{
    public int Id { get; set; }

    [Required]
    public int LiquidacionId { get; set; }

    [Required]
    public int DeudorId { get; set; }

    [Required]
    public int AcreedorId { get; set; }

    [Range(0.01, double.MaxValue)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Monto { get; set; }
}
