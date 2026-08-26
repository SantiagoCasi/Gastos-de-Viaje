using System.ComponentModel.DataAnnotations;
using GastosDeViaje.Models.Enums;

namespace GastosDeViaje.Models;

/// <summary>
/// Un viaje concreto que el organizador administra: agrupa participantes, gastos y
/// las liquidaciones (parciales o final) que se calculen sobre ellos.
/// </summary>
/// Cubre RF03, RF09.
public class SesionViaje
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Nombre del viaje")]
    public string Nombre { get; set; } = string.Empty;

    [Display(Name = "Estado")]
    public EstadoSesion Estado { get; set; } = EstadoSesion.Abierta;

    [Required]
    [Display(Name = "Fecha de creación")]
    public DateTime FechaCreacion { get; set; }

    [Display(Name = "Fecha de cierre")]
    public DateTime? FechaCierre { get; set; }

    /// <summary>
    /// Solo una etiqueta para mostrar en pantalla y en el PDF; no se hace ninguna
    /// conversión de moneda (fuera de alcance de la v1).
    /// </summary>
    [Required]
    [StringLength(3)]
    [Display(Name = "Moneda")]
    public string Moneda { get; set; } = "ARS";

    [Required]
    public string OrganizadorId { get; set; } = string.Empty;

    public ICollection<Participante> Participantes { get; set; } = new List<Participante>();
    public ICollection<Gasto> Gastos { get; set; } = new List<Gasto>();
    public ICollection<Liquidacion> Liquidaciones { get; set; } = new List<Liquidacion>();
}
