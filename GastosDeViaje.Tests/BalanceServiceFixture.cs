using GastosDeViaje.Data;
using GastosDeViaje.Models;
using Microsoft.EntityFrameworkCore;

namespace GastosDeViaje.Tests;

/// <summary>
/// Prepara una base de datos SQL Server dedicada a los tests ("GastosDeViajeTests",
/// misma instancia local que usa la app) y un organizador de prueba. Se comparte entre
/// todos los tests de <see cref="BalanceServiceTests"/> vía <see cref="ICollectionFixture{T}"/>
/// para no migrar el esquema en cada test.
/// </summary>
public class BalanceServiceFixture : IDisposable
{
    private const string CadenaConexion =
        "Server=localhost;Database=GastosDeViajeTests;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    public string OrganizadorId { get; }

    public BalanceServiceFixture()
    {
        using var contexto = CrearContexto();
        contexto.Database.EnsureCreated();

        var organizador = new ApplicationUser
        {
            UserName = "tests@balance.local",
            Email = "tests@balance.local",
            NombreCompleto = "Organizador de Tests"
        };

        if (!contexto.Users.Any(u => u.UserName == organizador.UserName))
        {
            contexto.Users.Add(organizador);
            contexto.SaveChanges();
        }

        OrganizadorId = contexto.Users.First(u => u.UserName == organizador.UserName).Id;
    }

    public AppDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(CadenaConexion)
            .Options;
        return new AppDbContext(opciones);
    }

    public void Dispose()
    {
        using var contexto = CrearContexto();
        contexto.Database.EnsureDeleted();
    }
}

[CollectionDefinition("Base de datos de tests")]
public class BaseDeDatosCollection : ICollectionFixture<BalanceServiceFixture>
{
}
