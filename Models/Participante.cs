using System.ComponentModel.DataAnnotations;

namespace GastosDeViaje.Models;

/// <summary>
/// Persona que viaja. En la v1 todos son "simulados": solo tienen nombre, no cuenta
/// propia. <see cref="UsuarioId"/> queda nullable, reservado para la v2 (participantes
/// reales con su propia cuenta).
/// </summary>
/// Cubre RF04, RF07.
public class Participante
{
    public int Id { get; set; }

    [Required]
    [StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    public bool EsSimulado { get; set; } = true;

    [Required]
    public int SesionViajeId { get; set; }

    /// <summary>Reservado para v2: cuenta real vinculada a este participante.</summary>
    public string? UsuarioId { get; set; }
}
