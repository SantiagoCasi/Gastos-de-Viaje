using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace GastosDeViaje.Models;

/// <summary>
/// Usuario real de la aplicación (el organizador de los viajes). Extiende el
/// <see cref="IdentityUser"/> de ASP.NET Core Identity únicamente con el dato que
/// Identity no provee de fábrica: el nombre completo de la persona.
/// </summary>
/// Cubre RF01, RF02.
public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(100)]
    public string NombreCompleto { get; set; } = string.Empty;
}
