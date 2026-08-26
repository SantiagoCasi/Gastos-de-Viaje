using System.ComponentModel.DataAnnotations;
using GastosDeViaje.Models.Enums;

namespace GastosDeViaje.ViewModels;

/// <summary>
/// Datos que el organizador completa al cargar o editar un gasto. Pensado para
/// completarse en una sola pantalla desde el celular (RNF04): pocos campos, sin pasos
/// intermedios. El estado de saldado (<c>LiquidacionId</c>) no se expone: lo asigna
/// únicamente <c>BalanceService</c> al calcular una liquidación.
/// </summary>
/// Cubre RF05, RF06, RF07.
public class GastoFormViewModel
{
    public int Id { get; set; }

    [Required]
    public int SesionViajeId { get; set; }

    [Required(ErrorMessage = "Elegí quién pagó el gasto.")]
    [Display(Name = "Pagado por")]
    public int ParticipanteId { get; set; }

    [Required(ErrorMessage = "Ingresá el monto del gasto.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto tiene que ser mayor a 0.")]
    [Display(Name = "Monto")]
    public decimal Monto { get; set; }

    [Required(ErrorMessage = "Ingresá la fecha del gasto.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha")]
    public DateTime Fecha { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Ingresá el lugar del gasto.")]
    [StringLength(120)]
    [Display(Name = "Lugar")]
    public string Lugar { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá el motivo del gasto.")]
    [StringLength(200)]
    [Display(Name = "Motivo")]
    public string Motivo { get; set; } = string.Empty;

    [Display(Name = "Método de pago")]
    public MetodoPago MetodoPago { get; set; }
}
