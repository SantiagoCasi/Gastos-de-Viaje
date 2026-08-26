using GastosDeViaje.Data;
using GastosDeViaje.Models;
using GastosDeViaje.Models.Enums;
using GastosDeViaje.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GastosDeViaje.Controllers;

/// <summary>
/// ABM de sesiones de viaje. Un organizador solo ve y administra sus propias sesiones:
/// toda consulta se filtra por el Id del usuario autenticado.
/// </summary>
/// Cubre RF03.
[Authorize]
public class SesionViajeController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public SesionViajeController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private string OrganizadorId => _userManager.GetUserId(User)!;

    // GET: SesionViaje
    public async Task<IActionResult> Index()
    {
        var sesiones = await _context.SesionesViaje
            .Where(s => s.OrganizadorId == OrganizadorId)
            .OrderByDescending(s => s.FechaCreacion)
            .ToListAsync();
        return View(sesiones);
    }

    // GET: SesionViaje/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        var sesionViaje = await BuscarPropiaAsync(id);
        if (sesionViaje == null)
        {
            return NotFound();
        }

        return View(sesionViaje);
    }

    // GET: SesionViaje/Create
    public IActionResult Create()
    {
        return View(new SesionViajeFormViewModel());
    }

    // POST: SesionViaje/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SesionViajeFormViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var sesionViaje = new SesionViaje
        {
            Nombre = modelo.Nombre,
            Moneda = modelo.Moneda,
            Estado = EstadoSesion.Abierta,
            FechaCreacion = DateTime.Now,
            OrganizadorId = OrganizadorId
        };
        _context.Add(sesionViaje);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: SesionViaje/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        var sesionViaje = await BuscarPropiaAsync(id);
        if (sesionViaje == null)
        {
            return NotFound();
        }

        return View(new SesionViajeFormViewModel
        {
            Id = sesionViaje.Id,
            Nombre = sesionViaje.Nombre,
            Moneda = sesionViaje.Moneda
        });
    }

    // POST: SesionViaje/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SesionViajeFormViewModel modelo)
    {
        if (id != modelo.Id)
        {
            return NotFound();
        }

        var sesionViaje = await BuscarPropiaAsync(id);
        if (sesionViaje == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        sesionViaje.Nombre = modelo.Nombre;
        sesionViaje.Moneda = modelo.Moneda;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: SesionViaje/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        var sesionViaje = await BuscarPropiaAsync(id);
        if (sesionViaje == null)
        {
            return NotFound();
        }

        return View(sesionViaje);
    }

    // POST: SesionViaje/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var sesionViaje = await BuscarPropiaAsync(id);
        if (sesionViaje != null)
        {
            _context.SesionesViaje.Remove(sesionViaje);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Busca una sesión por Id verificando que pertenezca al organizador logueado.</summary>
    private async Task<SesionViaje?> BuscarPropiaAsync(int? id)
    {
        if (id == null)
        {
            return null;
        }

        return await _context.SesionesViaje
            .FirstOrDefaultAsync(s => s.Id == id && s.OrganizadorId == OrganizadorId);
    }
}
