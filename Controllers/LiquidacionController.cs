using GastosDeViaje.Data;
using GastosDeViaje.Models;
using GastosDeViaje.Models.Enums;
using GastosDeViaje.Services;
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
    private readonly IComprobanteService _comprobanteService;
    private readonly UserManager<ApplicationUser> _userManager;

    public LiquidacionController(
        AppDbContext context,
        IBalanceService balanceService,
        IComprobanteService comprobanteService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _balanceService = balanceService;
        _comprobanteService = comprobanteService;
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
        if (!await PerteneceAlOrganizadorAsync(id))
        {
            return NotFound();
        }

        var detalle = await _balanceService.ObtenerDetalleAsync(id);
        return View(detalle);
    }

    // GET: Liquidacion/DescargarPdf/5
    public async Task<IActionResult> DescargarPdf(int id)
    {
        if (!await PerteneceAlOrganizadorAsync(id))
        {
            return NotFound();
        }

        var pdf = await _comprobanteService.GenerarPdfAsync(id);
        return File(pdf, "application/pdf", $"comprobante-liquidacion-{id}.pdf");
    }

    private async Task<bool> PerteneceAlOrganizadorAsync(int liquidacionId)
    {
        return await _context.Liquidaciones
            .AnyAsync(l => l.Id == liquidacionId && _context.SesionesViaje
                .Any(s => s.Id == l.SesionViajeId && s.OrganizadorId == OrganizadorId));
    }
}
