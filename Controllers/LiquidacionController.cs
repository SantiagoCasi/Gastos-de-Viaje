using GastosDeViaje.Data;
using GastosDeViaje.Models;
using GastosDeViaje.Models.Enums;
using GastosDeViaje.Services;
using GastosDeViaje.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GastosDeViaje.Controllers;

/// <summary>
/// Dispara el cálculo de liquidaciones (break y cierre final) sobre <see cref="IBalanceService"/>
/// y muestra el detalle matemático del resultado. No contiene lógica de negocio propia:
/// valida la pertenencia al organizador, delega el cálculo en el servicio y arma la vista.
/// </summary>
/// Cubre RF08, RF09, RF10.
[Authorize]
public class LiquidacionController : Controller
{
    private readonly AppDbContext _context;
    private readonly IBalanceService _balanceService;
    private readonly UserManager<ApplicationUser> _userManager;

    public LiquidacionController(AppDbContext context, IBalanceService balanceService, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _balanceService = balanceService;
        _userManager = userManager;
    }

    private string OrganizadorId => _userManager.GetUserId(User)!;

    // POST: Liquidacion/Break?sesionViajeId=5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Break(int sesionViajeId)
    {
        return await CalcularYRedirigirAsync(sesionViajeId, TipoLiquidacion.Parcial);
    }

    // POST: Liquidacion/Finalizar?sesionViajeId=5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finalizar(int sesionViajeId)
    {
        return await CalcularYRedirigirAsync(sesionViajeId, TipoLiquidacion.Final);
    }

    private async Task<IActionResult> CalcularYRedirigirAsync(int sesionViajeId, TipoLiquidacion tipo)
    {
        var sesionViaje = await _context.SesionesViaje
            .FirstOrDefaultAsync(s => s.Id == sesionViajeId && s.OrganizadorId == OrganizadorId);
        if (sesionViaje == null)
        {
            return NotFound();
        }

        try
        {
            var liquidacion = await _balanceService.CalcularLiquidacionAsync(sesionViajeId, tipo);
            return RedirectToAction(nameof(Detalle), new { id = liquidacion.Id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Details", "SesionViaje", new { id = sesionViajeId });
        }
    }

    // GET: Liquidacion/Detalle/5
    public async Task<IActionResult> Detalle(int id)
    {
        var liquidacion = await _context.Liquidaciones
            .FirstOrDefaultAsync(l => l.Id == id && _context.SesionesViaje
                .Any(s => s.Id == l.SesionViajeId && s.OrganizadorId == OrganizadorId));
        if (liquidacion == null)
        {
            return NotFound();
        }

        var sesionViaje = (await _context.SesionesViaje.FindAsync(liquidacion.SesionViajeId))!;

        var participantes = await _context.Participantes
            .Where(p => p.SesionViajeId == liquidacion.SesionViajeId)
            .OrderBy(p => p.Id)
            .ToListAsync();

        var pagadoPorParticipante = await _context.Gastos
            .Where(g => g.LiquidacionId == id)
            .GroupBy(g => g.ParticipanteId)
            .Select(grupo => new { ParticipanteId = grupo.Key, Total = grupo.Sum(g => g.Monto) })
            .ToDictionaryAsync(x => x.ParticipanteId, x => x.Total);

        var movimientos = await _context.MovimientosLiquidacion
            .Where(m => m.LiquidacionId == id)
            .ToListAsync();

        var saldoPorParticipante = participantes.ToDictionary(p => p.Id, _ => 0m);
        foreach (var movimiento in movimientos)
        {
            saldoPorParticipante[movimiento.AcreedorId] += movimiento.Monto;
            saldoPorParticipante[movimiento.DeudorId] -= movimiento.Monto;
        }

        var nombrePorParticipante = participantes.ToDictionary(p => p.Id, p => p.Nombre);

        var modelo = new DetalleLiquidacionViewModel
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
                return new FilaParticipanteViewModel
                {
                    Nombre = p.Nombre,
                    Pagado = pagado,
                    Cuota = pagado - saldo,
                    Saldo = saldo
                };
            }).ToList(),
            Transferencias = movimientos.Select(m => new TransferenciaViewModel
            {
                DeudorNombre = nombrePorParticipante[m.DeudorId],
                AcreedorNombre = nombrePorParticipante[m.AcreedorId],
                Monto = m.Monto
            }).ToList()
        };

        return View(modelo);
    }
}
