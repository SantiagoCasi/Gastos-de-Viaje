using System.ComponentModel.DataAnnotations;

namespace GastosDeViaje.ViewModels;

/// <summary>
/// Datos que el organizador completa al crear o editar una sesión de viaje. El
/// <c>Estado</c>, las fechas y el organizador los administra el controller: no forman
/// parte del formulario para que no puedan ser manipulados desde el cliente.
/// </summary>
/// Cubre RF03.
public class SesionViajeFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ingresá un nombre para el viaje.")]
    [StringLength(100)]
    [Display(Name = "Nombre del viaje")]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "La moneda debe tener 3 letras (ej: ARS).")]
    [Display(Name = "Moneda")]
    public string Moneda { get; set; } = "ARS";
}
