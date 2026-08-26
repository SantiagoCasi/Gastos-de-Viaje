using System.ComponentModel.DataAnnotations;

namespace GastosDeViaje.ViewModels;

/// <summary>
/// Datos que el organizador completa al agregar o editar un participante. En la v1
/// todos los participantes son simulados (sin cuenta propia), por eso ese campo no se
/// expone en el formulario: lo fija el controller.
/// </summary>
/// Cubre RF04, RF07.
public class ParticipanteFormViewModel
{
    public int Id { get; set; }

    [Required]
    public int SesionViajeId { get; set; }

    [Required(ErrorMessage = "Ingresá el nombre del participante.")]
    [StringLength(80)]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;
}
