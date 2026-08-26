using GastosDeViaje.Models;
using GastosDeViaje.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GastosDeViaje.Data;

/// <summary>
/// Carga de datos de ejemplo para poder probar el algoritmo de balance sin cargar todo
/// a mano: un organizador de demostración, una sesión de viaje, 4 participantes y 6
/// gastos. Es idempotente: si ya existe alguna sesión de viaje, no hace nada.
/// </summary>
public static class SeedData
{
    private const string EmailOrganizadorDemo = "organizador@demo.com";
    private const string PasswordOrganizadorDemo = "Demo123$";

    public static async Task InicializarAsync(IServiceProvider servicios)
    {
        var contexto = servicios.GetRequiredService<AppDbContext>();

        if (await contexto.SesionesViaje.AnyAsync())
        {
            return;
        }

        var userManager = servicios.GetRequiredService<UserManager<ApplicationUser>>();
        var organizador = await userManager.FindByEmailAsync(EmailOrganizadorDemo);
        if (organizador == null)
        {
            organizador = new ApplicationUser
            {
                UserName = EmailOrganizadorDemo,
                Email = EmailOrganizadorDemo,
                NombreCompleto = "Organizador Demo",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(organizador, PasswordOrganizadorDemo);
        }

        var sesion = new SesionViaje
        {
            Nombre = "Viaje a Bariloche",
            FechaCreacion = DateTime.Now,
            OrganizadorId = organizador.Id
        };
        contexto.SesionesViaje.Add(sesion);
        await contexto.SaveChangesAsync();

        var participantes = new List<Participante>
        {
            new() { Nombre = "Ana", SesionViajeId = sesion.Id },
            new() { Nombre = "Bruno", SesionViajeId = sesion.Id },
            new() { Nombre = "Carla", SesionViajeId = sesion.Id },
            new() { Nombre = "Diego", SesionViajeId = sesion.Id }
        };
        contexto.Participantes.AddRange(participantes);
        await contexto.SaveChangesAsync();

        var hoy = DateTime.Today;
        var gastos = new List<Gasto>
        {
            new() { SesionViajeId = sesion.Id, ParticipanteId = participantes[0].Id, Monto = 12000m, Fecha = hoy.AddDays(-3), Lugar = "Hostel Bariloche", Motivo = "Alojamiento", MetodoPago = MetodoPago.Transferencia },
            new() { SesionViajeId = sesion.Id, ParticipanteId = participantes[1].Id, Monto = 8000m, Fecha = hoy.AddDays(-3), Lugar = "Supermercado La Anónima", Motivo = "Comida", MetodoPago = MetodoPago.Debito },
            new() { SesionViajeId = sesion.Id, ParticipanteId = participantes[2].Id, Monto = 4500m, Fecha = hoy.AddDays(-2), Lugar = "Cerro Catedral", Motivo = "Entradas al cerro", MetodoPago = MetodoPago.Efectivo },
            new() { SesionViajeId = sesion.Id, ParticipanteId = participantes[3].Id, Monto = 6300m, Fecha = hoy.AddDays(-2), Lugar = "Estación de servicio", Motivo = "Nafta", MetodoPago = MetodoPago.Credito },
            new() { SesionViajeId = sesion.Id, ParticipanteId = participantes[0].Id, Monto = 3200m, Fecha = hoy.AddDays(-1), Lugar = "Restó El Boliche", Motivo = "Cena", MetodoPago = MetodoPago.Efectivo },
            new() { SesionViajeId = sesion.Id, ParticipanteId = participantes[1].Id, Monto = 1000m, Fecha = hoy, Lugar = "Kiosco", Motivo = "Snacks para el regreso", MetodoPago = MetodoPago.Efectivo }
        };
        contexto.Gastos.AddRange(gastos);
        await contexto.SaveChangesAsync();
    }
}
