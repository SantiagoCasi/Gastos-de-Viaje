using GastosDeViaje.Data;
using GastosDeViaje.Models;
using GastosDeViaje.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace GastosDeViaje.Services;

// Implementa el algoritmo de la sección 4 del prompt maestro: divide en partes iguales
// el total de los gastos pendientes de una sesión, ajusta el redondeo de centavos para
// que la suma dé exacta, y calcula el conjunto mínimo de transferencias que salda las
// cuentas. Todo el cálculo usa "decimal" para garantizar exactitud matemática (RNF06);
// no corre offline (ver sección 6 del prompt maestro).
/// <summary>
/// Implementación de <see cref="IBalanceService"/>.
/// </summary>
/// Cubre RF08, RF09, RF10, RNF06, RNF07.
public class BalanceService : IBalanceService
{
    private readonly AppDbContext _context;

    public BalanceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Liquidacion> CalcularLiquidacionAsync(int sesionViajeId, TipoLiquidacion tipo)
    {
        // 1) Gastos pendientes de la sesión.
        var gastosPendientes = await _context.Gastos
            .Where(g => g.SesionViajeId == sesionViajeId && g.LiquidacionId == null)
            .ToListAsync();

        if (gastosPendientes.Count == 0)
        {
            throw new InvalidOperationException("No hay gastos pendientes para liquidar en esta sesión.");
        }

        // 3) Participantes de la sesión, ordenados por Id (orden usado también para
        // repartir el ajuste de centavos, paso 5).
        var participantes = await _context.Participantes
            .Where(p => p.SesionViajeId == sesionViajeId)
            .OrderBy(p => p.Id)
            .ToListAsync();

        var n = participantes.Count;
        if (n < 2)
        {
            throw new InvalidOperationException("Hacen falta al menos 2 participantes para calcular una liquidación.");
        }

        // 2) Total y 4) cuota ideal, redondeada a 2 decimales.
        var total = gastosPendientes.Sum(g => g.Monto);
        var cuotaIdeal = Math.Round(total / n, 2, MidpointRounding.AwayFromZero);

        // 5) Ajuste de centavos: la diferencia entre el total exacto y la suma de cuotas
        // ideales se reparte de a $0.01 entre los primeros participantes (ordenados por
        // Id), hasta agotarla. Como todo es decimal con 2 decimales, el residuo siempre
        // es un múltiplo exacto de 0.01.
        var residuo = total - (cuotaIdeal * n);
        var pasosDeAjuste = (int)Math.Round(Math.Abs(residuo) / 0.01m, MidpointRounding.AwayFromZero);
        var signoAjuste = residuo >= 0 ? 1m : -1m;

        var cuotaPorParticipante = new Dictionary<int, decimal>();
        for (var i = 0; i < n; i++)
        {
            var ajuste = i < pasosDeAjuste ? 0.01m * signoAjuste : 0m;
            cuotaPorParticipante[participantes[i].Id] = cuotaIdeal + ajuste;
        }

        // 6) Pagado y saldo de cada participante.
        var pagadoPorParticipante = participantes.ToDictionary(p => p.Id, _ => 0m);
        foreach (var gasto in gastosPendientes)
        {
            pagadoPorParticipante[gasto.ParticipanteId] += gasto.Monto;
        }

        var saldoPorParticipante = participantes.ToDictionary(
            p => p.Id,
            p => pagadoPorParticipante[p.Id] - cuotaPorParticipante[p.Id]);

        // 7) Verificación obligatoria: la suma de los saldos tiene que dar exactamente 0.
        if (saldoPorParticipante.Values.Sum() != 0m)
        {
            throw new InvalidOperationException(
                "La suma de los saldos no dio exactamente 0: no se guardó ningún cambio.");
        }

        // 8) Minimización de transferencias.
        var movimientos = MinimizarTransferencias(saldoPorParticipante);

        // 9) Persistencia: liquidación + movimientos + marcar los gastos como saldados,
        // todo en una única transacción.
        await using var transaccion = await _context.Database.BeginTransactionAsync();

        var liquidacion = new Liquidacion
        {
            SesionViajeId = sesionViajeId,
            Fecha = DateTime.Now,
            Tipo = tipo,
            TotalGastado = total,
            CantidadParticipantes = n,
            CuotaIdeal = cuotaIdeal
        };
        _context.Liquidaciones.Add(liquidacion);
        await _context.SaveChangesAsync();

        foreach (var (deudorId, acreedorId, monto) in movimientos)
        {
            _context.MovimientosLiquidacion.Add(new MovimientoLiquidacion
            {
                LiquidacionId = liquidacion.Id,
                DeudorId = deudorId,
                AcreedorId = acreedorId,
                Monto = monto
            });
        }

        foreach (var gasto in gastosPendientes)
        {
            gasto.LiquidacionId = liquidacion.Id;
        }

        // 10) Según el tipo: el break deja la sesión Abierta; el final la cierra.
        if (tipo == TipoLiquidacion.Final)
        {
            var sesionViaje = await _context.SesionesViaje.FindAsync(sesionViajeId)
                ?? throw new InvalidOperationException("La sesión de viaje no existe.");
            sesionViaje.Estado = EstadoSesion.Cerrada;
            sesionViaje.FechaCierre = DateTime.Now;
        }

        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();

        return liquidacion;
    }

    public async Task<ResultadoLiquidacion> ObtenerDetalleAsync(int liquidacionId)
    {
        var liquidacion = await _context.Liquidaciones.FindAsync(liquidacionId)
            ?? throw new InvalidOperationException("La liquidación no existe.");
        var sesionViaje = await _context.SesionesViaje.FindAsync(liquidacion.SesionViajeId)
            ?? throw new InvalidOperationException("La sesión de viaje no existe.");

        var participantes = await _context.Participantes
            .Where(p => p.SesionViajeId == liquidacion.SesionViajeId)
            .OrderBy(p => p.Id)
            .ToListAsync();

        // Pagado_i: suma de los gastos de esta liquidación donde el participante figura como pagador.
        var pagadoPorParticipante = await _context.Gastos
            .Where(g => g.LiquidacionId == liquidacionId)
            .GroupBy(g => g.ParticipanteId)
            .Select(grupo => new { ParticipanteId = grupo.Key, Total = grupo.Sum(g => g.Monto) })
            .ToDictionaryAsync(x => x.ParticipanteId, x => x.Total);

        var movimientos = await _context.MovimientosLiquidacion
            .Where(m => m.LiquidacionId == liquidacionId)
            .ToListAsync();

        // Saldo_i se reconstruye a partir de los movimientos ya persistidos: participante
        // acreedor suma, deudor resta. Cuota_i = Pagado_i - Saldo_i (despejando la fórmula
        // del paso 6 del algoritmo). No hace falta volver a correr el redondeo de centavos.
        var saldoPorParticipante = participantes.ToDictionary(p => p.Id, _ => 0m);
        foreach (var movimiento in movimientos)
        {
            saldoPorParticipante[movimiento.AcreedorId] += movimiento.Monto;
            saldoPorParticipante[movimiento.DeudorId] -= movimiento.Monto;
        }

        var nombrePorParticipante = participantes.ToDictionary(p => p.Id, p => p.Nombre);

        var gastosIncluidos = await _context.Gastos
            .Where(g => g.LiquidacionId == liquidacionId)
            .OrderBy(g => g.Fecha)
            .ToListAsync();
        var gastos = gastosIncluidos.Select(g => new GastoIncluido
        {
            Fecha = g.Fecha,
            Lugar = g.Lugar,
            Motivo = g.Motivo,
            ParticipanteNombre = nombrePorParticipante[g.ParticipanteId],
            Monto = g.Monto
        }).ToList();

        return new ResultadoLiquidacion
        {
            LiquidacionId = liquidacion.Id,
            SesionViajeId = sesionViaje.Id,
            SesionNombre = sesionViaje.Nombre,
            Moneda = sesionViaje.Moneda,
            Tipo = liquidacion.Tipo,
            Fecha = liquidacion.Fecha,
            TotalGastado = liquidacion.TotalGastado,
            CantidadParticipantes = liquidacion.CantidadParticipantes,
            CuotaIdeal = liquidacion.CuotaIdeal,
            Participantes = participantes.Select(p =>
            {
                var pagado = pagadoPorParticipante.GetValueOrDefault(p.Id);
                var saldo = saldoPorParticipante[p.Id];
                return new BalanceParticipante
                {
                    Nombre = p.Nombre,
                    Pagado = pagado,
                    Cuota = pagado - saldo,
                    Saldo = saldo
                };
            }).ToList(),
            Transferencias = movimientos.Select(m => new Transferencia
            {
                DeudorNombre = nombrePorParticipante[m.DeudorId],
                AcreedorNombre = nombrePorParticipante[m.AcreedorId],
                Monto = m.Monto
            }).ToList(),
            Gastos = gastos
        };
    }

    /// <summary>
    /// Ordena a los acreedores (saldo positivo) de mayor a menor y a los deudores
    /// (saldo negativo) de mayor a menor deuda, y va cruzando el mayor deudor con el
    /// mayor acreedor hasta saldar a todos. Es el algoritmo greedy que produce la
    /// menor cantidad posible de transferencias (RNF07).
    /// </summary>
    private static List<(int DeudorId, int AcreedorId, decimal Monto)> MinimizarTransferencias(
        Dictionary<int, decimal> saldoPorParticipante)
    {
        var acreedores = new List<(int Id, decimal Saldo)>();
        var deudores = new List<(int Id, decimal Saldo)>();
        foreach (var (id, saldo) in saldoPorParticipante)
        {
            if (saldo > 0m)
            {
                acreedores.Add((id, saldo));
            }
            else if (saldo < 0m)
            {
                deudores.Add((id, -saldo));
            }
        }

        acreedores = acreedores.OrderByDescending(a => a.Saldo).ToList();
        deudores = deudores.OrderByDescending(d => d.Saldo).ToList();

        var movimientos = new List<(int, int, decimal)>();
        var i = 0;
        var j = 0;
        while (i < deudores.Count && j < acreedores.Count)
        {
            var deudor = deudores[i];
            var acreedor = acreedores[j];
            var monto = Math.Min(deudor.Saldo, acreedor.Saldo);

            movimientos.Add((deudor.Id, acreedor.Id, monto));

            deudores[i] = (deudor.Id, deudor.Saldo - monto);
            acreedores[j] = (acreedor.Id, acreedor.Saldo - monto);

            if (deudores[i].Saldo == 0m)
            {
                i++;
            }

            if (acreedores[j].Saldo == 0m)
            {
                j++;
            }
        }

        return movimientos;
    }
}
