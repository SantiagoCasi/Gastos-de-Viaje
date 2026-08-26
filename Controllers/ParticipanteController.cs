using GastosDeViaje.Data;
using GastosDeViaje.Models;
using GastosDeViaje.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GastosDeViaje.Controllers;

/// <summary>
/// ABM de participantes de una sesión de viaje. Todas las acciones verifican que la
/// sesión pertenezca al organizador logueado antes de leer o modificar nada.
/// </summary>
/// Cubre RF04, RF07.
[Authorize]
public class ParticipanteController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ParticipanteController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private string OrganizadorId => _userManager.GetUserId(User)!;

    // GET: Participante?sesionViajeId=5
    public async Task<IActionResult> Index(int sesionViajeId)
    {
        var sesionViaje = await BuscarSesionPropiaAsync(sesionViajeId);
        if (sesionViaje == null)
        {
            return NotFound();
        }

        ViewBag.SesionViaje = sesionViaje;
        var participantes = await _context.Participantes
            .Where(p => p.SesionViajeId == sesionViajeId)
            .OrderBy(p => p.Id)
            .ToListAsync();
        return View(participantes);
    }

    // GET: Participante/Create?sesionViajeId=5
    public async Task<IActionResult> Create(int sesionViajeId)
    {
        var sesionViaje = await BuscarSesionPropiaAsync(sesionViajeId);
        if (sesionViaje == null)
        {
            return NotFound();
        }

        ViewBag.SesionViaje = sesionViaje;
        return View(new ParticipanteFormViewModel { SesionViajeId = sesionViajeId });
    }

    // POST: Participante/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ParticipanteFormViewModel modelo)
    {
        var sesionViaje = await BuscarSesionPropiaAsync(modelo.SesionViajeId);
        if (sesionViaje == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.SesionViaje = sesionViaje;
            return View(modelo);
        }

        var participante = new Participante
        {
            Nombre = modelo.Nombre,
            SesionViajeId = modelo.SesionViajeId,
            EsSimulado = true
        };
        _context.Add(participante);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { sesionViajeId = modelo.SesionViajeId });
    }

    // GET: Participante/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        var participante = await BuscarPropioAsync(id);
        if (participante == null)
        {
            return NotFound();
        }

        ViewBag.SesionViaje = await _context.SesionesViaje.FindAsync(participante.SesionViajeId);
        return View(new ParticipanteFormViewModel
        {
            Id = participante.Id,
            SesionViajeId = participante.SesionViajeId,
            Nombre = participante.Nombre
        });
    }

    // POST: Participante/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ParticipanteFormViewModel modelo)
    {
        if (id != modelo.Id)
        {
            return NotFound();
        }

        var participante = await BuscarPropioAsync(id);
        if (participante == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.SesionViaje = await _context.SesionesViaje.FindAsync(participante.SesionViajeId);
            return View(modelo);
        }

        participante.Nombre = modelo.Nombre;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { sesionViajeId = participante.SesionViajeId });
    }

    // GET: Participante/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        var participante = await BuscarPropioAsync(id);
        if (participante == null)
        {
            return NotFound();
        }

        ViewBag.SesionViaje = await _context.SesionesViaje.FindAsync(participante.SesionViajeId);
        return View(participante);
    }

    // POST: Participante/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var participante = await BuscarPropioAsync(id);
        if (participante == null)
        {
            return NotFound();
        }

        var sesionViajeId = participante.SesionViajeId;
        _context.Participantes.Remove(participante);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { sesionViajeId });
    }

    /// <summary>Busca una sesión de viaje verificando que pertenezca al organizador logueado.</summary>
    private async Task<SesionViaje?> BuscarSesionPropiaAsync(int sesionViajeId)
    {
        return await _context.SesionesViaje
            .FirstOrDefaultAsync(s => s.Id == sesionViajeId && s.OrganizadorId == OrganizadorId);
    }

    /// <summary>Busca un participante verificando que su sesión pertenezca al organizador logueado.</summary>
    private async Task<Participante?> BuscarPropioAsync(int? id)
    {
        if (id == null)
        {
            return null;
        }

        return await _context.Participantes
            .FirstOrDefaultAsync(p => p.Id == id && _context.SesionesViaje
                .Any(s => s.Id == p.SesionViajeId && s.OrganizadorId == OrganizadorId));
    }
}
