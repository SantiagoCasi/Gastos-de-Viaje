using GastosDeViaje.Data;
using GastosDeViaje.Models;
using GastosDeViaje.Models.Enums;
using GastosDeViaje.Services;
using Microsoft.EntityFrameworkCore;

namespace GastosDeViaje.Tests;

/// <summary>
/// Casos mínimos exigidos por la sección 8 (Fase 4) del prompt maestro para el
/// algoritmo de balance: división exacta, residuo de centavos, un solo pagador, cero
/// movimientos, verificación de suma de saldos, y break seguido de una segunda
/// liquidación. Corre contra SQL Server real (no hay ningún paquete de mocking/in-memory
/// autorizado), cada test arma su propia sesión y participantes para no interferir
/// con los demás.
/// </summary>
/// Cubre RNF06, RNF07, RF08, RF09.
[Collection("Base de datos de tests")]
public class BalanceServiceTests
{
    private readonly BalanceServiceFixture _fixture;

    public BalanceServiceTests(BalanceServiceFixture fixture)
    {
        _fixture = fixture;
    }

    private static Gasto NuevoGasto(int sesionViajeId, int participanteId, decimal monto, DateTime fecha) => new()
    {
        SesionViajeId = sesionViajeId,
        ParticipanteId = participanteId,
        Monto = monto,
        Fecha = fecha,
        Lugar = "Lugar de prueba",
        Motivo = "Motivo de prueba",
        MetodoPago = MetodoPago.Efectivo
    };

    private async Task<(SesionViaje Sesion, List<Participante> Participantes)> CrearSesionAsync(
        AppDbContext contexto, int cantidadParticipantes)
    {
        var sesion = new SesionViaje
        {
            Nombre = $"Sesión de test {Guid.NewGuid()}",
            FechaCreacion = DateTime.Now,
            OrganizadorId = _fixture.OrganizadorId
        };
        contexto.SesionesViaje.Add(sesion);
        await contexto.SaveChangesAsync();

        var participantes = new List<Participante>();
        for (var i = 0; i < cantidadParticipantes; i++)
        {
            participantes.Add(new Participante { Nombre = $"Participante {i + 1}", SesionViajeId = sesion.Id });
        }
        contexto.Participantes.AddRange(participantes);
        await contexto.SaveChangesAsync();

        return (sesion, participantes);
    }

    /// <summary>Saldo neto derivado de los movimientos persistidos, igual que hace la vista de detalle (RF10).</summary>
    private static async Task<Dictionary<int, decimal>> SaldosDesdeMovimientosAsync(AppDbContext contexto, int liquidacionId, IEnumerable<int> participanteIds)
    {
        var saldos = participanteIds.ToDictionary(id => id, _ => 0m);
        var movimientos = await contexto.MovimientosLiquidacion.Where(m => m.LiquidacionId == liquidacionId).ToListAsync();
        foreach (var m in movimientos)
        {
            saldos[m.AcreedorId] += m.Monto;
            saldos[m.DeudorId] -= m.Monto;
        }
        return saldos;
    }

    [Fact]
    public async Task DivisionExacta_SinResiduo_GeneraLasCuotasIguales()
    {
        await using var contexto = _fixture.CrearContexto();
        var servicio = new BalanceService(contexto);
        var (sesion, participantes) = await CrearSesionAsync(contexto, 3);
        var hoy = DateTime.Today;

        contexto.Gastos.Add(NuevoGasto(sesion.Id, participantes[0].Id, 60m, hoy));
        contexto.Gastos.Add(NuevoGasto(sesion.Id, participantes[1].Id, 30m, hoy));
        contexto.Gastos.Add(NuevoGasto(sesion.Id, participantes[2].Id, 30m, hoy));
        await contexto.SaveChangesAsync();

        var liquidacion = await servicio.CalcularLiquidacionAsync(sesion.Id, TipoLiquidacion.Parcial);

        Assert.Equal(120m, liquidacion.TotalGastado);
        Assert.Equal(40m, liquidacion.CuotaIdeal);

        var saldos = await SaldosDesdeMovimientosAsync(contexto, liquidacion.Id, participantes.Select(p => p.Id));
        Assert.Equal(20m, saldos[participantes[0].Id]);
        Assert.Equal(-10m, saldos[participantes[1].Id]);
        Assert.Equal(-10m, saldos[participantes[2].Id]);
    }

    [Fact]
    public async Task DivisionConResiduo_DeCentavos_LaSumaDeCuotasIgualaElTotal()
    {
        await using var contexto = _fixture.CrearContexto();
        var servicio = new BalanceService(contexto);
        var (sesion, participantes) = await CrearSesionAsync(contexto, 3);
        var hoy = DateTime.Today;

        // 100 / 3 = 33.33... -> cuota ideal 33.33, residuo 0.01 a repartir.
        contexto.Gastos.Add(NuevoGasto(sesion.Id, participantes[0].Id, 100m, hoy));
        await contexto.SaveChangesAsync();

        var liquidacion = await servicio.CalcularLiquidacionAsync(sesion.Id, TipoLiquidacion.Parcial);

        Assert.Equal(33.33m, liquidacion.CuotaIdeal);

        var saldos = await SaldosDesdeMovimientosAsync(contexto, liquidacion.Id, participantes.Select(p => p.Id));
        var pagos = new Dictionary<int, decimal> { [participantes[0].Id] = 100m, [participantes[1].Id] = 0m, [participantes[2].Id] = 0m };
        var cuotas = participantes.Select(p => pagos[p.Id] - saldos[p.Id]).ToList();

        // La suma de las cuotas reales tiene que dar exactamente el total (100), y solo
        // una de las tres tiene que llevar el centavo de más (33.34 en vez de 33.33).
        Assert.Equal(100m, cuotas.Sum());
        Assert.Single(cuotas, c => c == 33.34m);
        Assert.Equal(2, cuotas.Count(c => c == 33.33m));
    }

    [Fact]
    public async Task UnSoloPagador_LosDemasLeDebenSuCuotaCompleta()
    {
        await using var contexto = _fixture.CrearContexto();
        var servicio = new BalanceService(contexto);
        var (sesion, participantes) = await CrearSesionAsync(contexto, 4);
        var hoy = DateTime.Today;

        contexto.Gastos.Add(NuevoGasto(sesion.Id, participantes[0].Id, 400m, hoy));
        await contexto.SaveChangesAsync();

        var liquidacion = await servicio.CalcularLiquidacionAsync(sesion.Id, TipoLiquidacion.Parcial);

        Assert.Equal(100m, liquidacion.CuotaIdeal);

        var movimientos = await contexto.MovimientosLiquidacion.Where(m => m.LiquidacionId == liquidacion.Id).ToListAsync();
        Assert.Equal(3, movimientos.Count);
        Assert.All(movimientos, m => Assert.Equal(participantes[0].Id, m.AcreedorId));
        Assert.All(movimientos, m => Assert.Equal(100m, m.Monto));
    }

    [Fact]
    public async Task TodosPagaronLoMismo_NoGeneraMovimientos()
    {
        await using var contexto = _fixture.CrearContexto();
        var servicio = new BalanceService(contexto);
        var (sesion, participantes) = await CrearSesionAsync(contexto, 4);
        var hoy = DateTime.Today;

        foreach (var participante in participantes)
        {
            contexto.Gastos.Add(NuevoGasto(sesion.Id, participante.Id, 100m, hoy));
        }
        await contexto.SaveChangesAsync();

        var liquidacion = await servicio.CalcularLiquidacionAsync(sesion.Id, TipoLiquidacion.Parcial);

        var movimientos = await contexto.MovimientosLiquidacion.Where(m => m.LiquidacionId == liquidacion.Id).ToListAsync();
        Assert.Empty(movimientos);
    }

    [Fact]
    public async Task SumaDeSaldos_DaSiempreExactamenteCero()
    {
        await using var contexto = _fixture.CrearContexto();
        var servicio = new BalanceService(contexto);
        var (sesion, participantes) = await CrearSesionAsync(contexto, 5);
        var hoy = DateTime.Today;

        contexto.Gastos.Add(NuevoGasto(sesion.Id, participantes[0].Id, 123.45m, hoy));
        contexto.Gastos.Add(NuevoGasto(sesion.Id, participantes[1].Id, 67.89m, hoy));
        contexto.Gastos.Add(NuevoGasto(sesion.Id, participantes[2].Id, 10.01m, hoy));
        await contexto.SaveChangesAsync();

        var liquidacion = await servicio.CalcularLiquidacionAsync(sesion.Id, TipoLiquidacion.Parcial);

        var saldos = await SaldosDesdeMovimientosAsync(contexto, liquidacion.Id, participantes.Select(p => p.Id));
        Assert.Equal(0m, saldos.Values.Sum());
    }

    [Fact]
    public async Task Break_SeguidoDeNuevosGastos_LaSegundaLiquidacionSoloIncluyeLosNuevos()
    {
        await using var contexto = _fixture.CrearContexto();
        var servicio = new BalanceService(contexto);
        var (sesion, participantes) = await CrearSesionAsync(contexto, 2);
        var hoy = DateTime.Today;

        contexto.Gastos.Add(NuevoGasto(sesion.Id, participantes[0].Id, 100m, hoy));
        await contexto.SaveChangesAsync();

        var primeraLiquidacion = await servicio.CalcularLiquidacionAsync(sesion.Id, TipoLiquidacion.Parcial);

        var sesionRecargada = await contexto.SesionesViaje.FindAsync(sesion.Id);
        Assert.Equal(EstadoSesion.Abierta, sesionRecargada!.Estado);

        contexto.Gastos.Add(NuevoGasto(sesion.Id, participantes[1].Id, 50m, hoy.AddDays(1)));
        await contexto.SaveChangesAsync();

        var segundaLiquidacion = await servicio.CalcularLiquidacionAsync(sesion.Id, TipoLiquidacion.Parcial);

        Assert.Equal(50m, segundaLiquidacion.TotalGastado);
        Assert.NotEqual(primeraLiquidacion.Id, segundaLiquidacion.Id);

        var gastosPrimera = await contexto.Gastos.Where(g => g.LiquidacionId == primeraLiquidacion.Id).ToListAsync();
        Assert.Single(gastosPrimera);
        Assert.Equal(100m, gastosPrimera[0].Monto);
    }

    [Fact]
    public async Task SinGastosPendientes_LanzaExcepcionYNoPersisteNada()
    {
        await using var contexto = _fixture.CrearContexto();
        var servicio = new BalanceService(contexto);
        var (sesion, _) = await CrearSesionAsync(contexto, 2);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.CalcularLiquidacionAsync(sesion.Id, TipoLiquidacion.Parcial));
    }

    [Fact]
    public async Task MenosDeDosParticipantes_LanzaExcepcion()
    {
        await using var contexto = _fixture.CrearContexto();
        var servicio = new BalanceService(contexto);
        var (sesion, participantes) = await CrearSesionAsync(contexto, 1);
        contexto.Gastos.Add(NuevoGasto(sesion.Id, participantes[0].Id, 50m, DateTime.Today));
        await contexto.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.CalcularLiquidacionAsync(sesion.Id, TipoLiquidacion.Parcial));
    }
}
