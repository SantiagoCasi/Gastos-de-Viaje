using GastosDeViaje.Data;
using GastosDeViaje.Models;
using GastosDeViaje.Models.Enums;
using GastosDeViaje.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GastosDeViaje.Controllers;

/// <summary>
/// ABM de gastos de una sesión de viaje. Solo permite cargar o editar gastos mientras
/// la sesión esté <see cref="EstadoSesion.Abierta"/>: una vez cerrada (RF09) no admite
/// gastos nuevos.
/// </summary>
/// Cubre RF05, RF06, RF07.
[Authorize]
public class GastoController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public GastoController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private string OrganizadorId => _userManager.GetUserId(User)!;

    // GET: Gasto?sesionViajeId=5
    public async Task<IActionResult> Index(int sesionViajeId)
    {
        var sesionViaje = await BuscarSesionPropiaAsync(sesionViajeId);
        if (sesionViaje == null)
        {
            return NotFound();
        }

        ViewBag.SesionViaje = sesionViaje;

        var gastos = await _context.Gastos
            .Where(g => g.SesionViajeId == sesionViajeId)
            .Join(_context.Participantes, g => g.ParticipanteId, p => p.Id, (g, p) => new GastoListItemViewModel
            {
                Id = g.Id,
                Monto = g.Monto,
                Fecha = g.Fecha,
                Lugar = g.Lugar,
                Motivo = g.Motivo,
                MetodoPago = g.MetodoPago,
                ParticipanteNombre = p.Nombre,
                Saldado = g.LiquidacionId != null
            })
            .OrderByDescending(g => g.Fecha)
            .ToListAsync();
        return View(gastos);
    }

    // GET: Gasto/Create?sesionViajeId=5
    public async Task<IActionResult> Create(int sesionViajeId)
    {
        var sesionViaje = await BuscarSesionPropiaAsync(sesionViajeId);
        if (sesionViaje == null)
        {
            return NotFound();
        }

        if (sesionViaje.Estado == EstadoSesion.Cerrada)
        {
            TempData["Error"] = "Esta sesión está cerrada: no se pueden cargar más gastos.";
            return RedirectToAction(nameof(Index), new { sesionViajeId });
        }

        await CargarListasAsync(sesionViaje);
        return View(new GastoFormViewModel { SesionViajeId = sesionViajeId });
    }

    // POST: Gasto/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GastoFormViewModel modelo)
    {
        var sesionViaje = await BuscarSesionPropiaAsync(modelo.SesionViajeId);
        if (sesionViaje == null)
        {
            return NotFound();
        }

        if (sesionViaje.Estado == EstadoSesion.Cerrada)
        {
            TempData["Error"] = "Esta sesión está cerrada: no se pueden cargar más gastos.";
            return RedirectToAction(nameof(Index), new { sesionViajeId = modelo.SesionViajeId });
        }

        if (!await ParticipanteValidoAsync(modelo.ParticipanteId, modelo.SesionViajeId))
        {
            ModelState.AddModelError(nameof(modelo.ParticipanteId), "El participante elegido no pertenece a esta sesión.");
        }

        if (!ModelState.IsValid)
        {
            await CargarListasAsync(sesionViaje);
            return View(modelo);
        }

        var gasto = new Gasto
        {
            SesionViajeId = modelo.SesionViajeId,
            ParticipanteId = modelo.ParticipanteId,
            Monto = modelo.Monto,
            Fecha = modelo.Fecha,
            Lugar = modelo.Lugar,
            Motivo = modelo.Motivo,
            MetodoPago = modelo.MetodoPago
        };
        _context.Add(gasto);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { sesionViajeId = modelo.SesionViajeId });
    }

    // GET: Gasto/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        var gasto = await BuscarPropioAsync(id);
        if (gasto == null)
        {
            return NotFound();
        }

        var sesionViaje = (await _context.SesionesViaje.FindAsync(gasto.SesionViajeId))!;
        if (sesionViaje.Estado == EstadoSesion.Cerrada)
        {
            TempData["Error"] = "Esta sesión está cerrada: no se pueden editar sus gastos.";
            return RedirectToAction(nameof(Index), new { sesionViajeId = gasto.SesionViajeId });
        }

        await CargarListasAsync(sesionViaje);
        return View(new GastoFormViewModel
        {
            Id = gasto.Id,
            SesionViajeId = gasto.SesionViajeId,
            ParticipanteId = gasto.ParticipanteId,
            Monto = gasto.Monto,
            Fecha = gasto.Fecha,
            Lugar = gasto.Lugar,
            Motivo = gasto.Motivo,
            MetodoPago = gasto.MetodoPago
        });
    }

    // POST: Gasto/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, GastoFormViewModel modelo)
    {
        if (id != modelo.Id)
        {
            return NotFound();
        }

        var gasto = await BuscarPropioAsync(id);
        if (gasto == null)
        {
            return NotFound();
        }

        var sesionViaje = (await _context.SesionesViaje.FindAsync(gasto.SesionViajeId))!;
        if (sesionViaje.Estado == EstadoSesion.Cerrada)
        {
            TempData["Error"] = "Esta sesión está cerrada: no se pueden editar sus gastos.";
            return RedirectToAction(nameof(Index), new { sesionViajeId = gasto.SesionViajeId });
        }

        if (!await ParticipanteValidoAsync(modelo.ParticipanteId, gasto.SesionViajeId))
        {
            ModelState.AddModelError(nameof(modelo.ParticipanteId), "El participante elegido no pertenece a esta sesión.");
        }

        if (!ModelState.IsValid)
        {
            await CargarListasAsync(sesionViaje);
            return View(modelo);
        }

        gasto.ParticipanteId = modelo.ParticipanteId;
        gasto.Monto = modelo.Monto;
        gasto.Fecha = modelo.Fecha;
        gasto.Lugar = modelo.Lugar;
        gasto.Motivo = modelo.Motivo;
        gasto.MetodoPago = modelo.MetodoPago;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { sesionViajeId = gasto.SesionViajeId });
    }

    // GET: Gasto/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        var gasto = await BuscarPropioAsync(id);
        if (gasto == null)
        {
            return NotFound();
        }

        ViewBag.Participante = await _context.Participantes.FindAsync(gasto.ParticipanteId);
        return View(gasto);
    }

    // POST: Gasto/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var gasto = await BuscarPropioAsync(id);
        if (gasto == null)
        {
            return NotFound();
        }

        var sesionViajeId = gasto.SesionViajeId;
        _context.Gastos.Remove(gasto);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { sesionViajeId });
    }

    /// <summary>Busca una sesión de viaje verificando que pertenezca al organizador logueado.</summary>
    private async Task<SesionViaje?> BuscarSesionPropiaAsync(int sesionViajeId)
    {
        return await _context.SesionesViaje
            .FirstOrDefaultAsync(s => s.Id == sesionViajeId && s.OrganizadorId == OrganizadorId);
    }

    /// <summary>Busca un gasto verificando que su sesión pertenezca al organizador logueado.</summary>
    private async Task<Gasto?> BuscarPropioAsync(int? id)
    {
        if (id == null)
        {
            return null;
        }

        return await _context.Gastos
            .FirstOrDefaultAsync(g => g.Id == id && _context.SesionesViaje
                .Any(s => s.Id == g.SesionViajeId && s.OrganizadorId == OrganizadorId));
    }

    private async Task<bool> ParticipanteValidoAsync(int participanteId, int sesionViajeId)
    {
        return await _context.Participantes.AnyAsync(p => p.Id == participanteId && p.SesionViajeId == sesionViajeId);
    }

    private async Task CargarListasAsync(SesionViaje sesionViaje)
    {
        ViewBag.SesionViaje = sesionViaje;
        var participantes = await _context.Participantes
            .Where(p => p.SesionViajeId == sesionViaje.Id)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
        ViewBag.Participantes = new SelectList(participantes, nameof(Participante.Id), nameof(Participante.Nombre));
        ViewBag.MetodosPago = new SelectList(Enum.GetValues<MetodoPago>());
    }
}
