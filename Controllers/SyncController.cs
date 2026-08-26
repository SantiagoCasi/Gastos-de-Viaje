using GastosDeViaje.Data;
using GastosDeViaje.Models;
using GastosDeViaje.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GastosDeViaje.Controllers;

/// <summary>
/// Único endpoint JSON de la aplicación (sección 6 del prompt maestro: no hay una Web
/// API completa, solo este). Recibe la cola de gastos que <c>offline.js</c> guardó en
/// IndexedDB mientras no había conexión y los persiste, devolviendo qué id temporal
/// quedó asociado a qué id real de servidor para que el cliente pueda vaciar su cola.
/// </summary>
/// Cubre RF06, RNF01, RNF03.
[Authorize]
[ApiController]
[Route("api/sync/gastos")]
public class SyncController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public SyncController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public class GastoOfflineDto
    {
        public string IdTemporal { get; set; } = string.Empty;
        public int SesionViajeId { get; set; }
        public int ParticipanteId { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public string Lugar { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public MetodoPago MetodoPago { get; set; }
    }

    public class GastoSincronizadoDto
    {
        public string IdTemporal { get; set; } = string.Empty;
        public int IdServidor { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Gastos([FromBody] List<GastoOfflineDto> gastos)
    {
        var organizadorId = _userManager.GetUserId(User)!;
        var confirmados = new List<GastoSincronizadoDto>();

        foreach (var dto in gastos)
        {
            // Se revalida todo del lado del servidor: la cola offline pudo haberse
            // armado con datos viejos (sesión ya cerrada, participante borrado, etc.).
            var sesionValida = await _context.SesionesViaje.AnyAsync(s =>
                s.Id == dto.SesionViajeId && s.OrganizadorId == organizadorId && s.Estado == EstadoSesion.Abierta);
            var participanteValido = await _context.Participantes.AnyAsync(p =>
                p.Id == dto.ParticipanteId && p.SesionViajeId == dto.SesionViajeId);

            if (!sesionValida || !participanteValido || dto.Monto <= 0)
            {
                continue;
            }

            var gasto = new Gasto
            {
                SesionViajeId = dto.SesionViajeId,
                ParticipanteId = dto.ParticipanteId,
                Monto = dto.Monto,
                Fecha = dto.Fecha,
                Lugar = dto.Lugar,
                Motivo = dto.Motivo,
                MetodoPago = dto.MetodoPago
            };
            _context.Gastos.Add(gasto);
            await _context.SaveChangesAsync();

            confirmados.Add(new GastoSincronizadoDto { IdTemporal = dto.IdTemporal, IdServidor = gasto.Id });
        }

        return Ok(confirmados);
    }
}
