using GastosDeViaje.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GastosDeViaje.Data;

/// <summary>
/// Contexto de EF Core de la aplicación. Extiende <see cref="IdentityDbContext{TUser}"/>
/// para incluir, además de las tablas propias de Identity, el modelo de dominio de
/// Gastos de Viaje (agregado en la Fase 2).
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
}
