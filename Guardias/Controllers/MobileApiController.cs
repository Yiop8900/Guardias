using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Guardias.Data;
using Guardias.Models;

namespace Guardias.Controllers;

[Authorize(Roles = "Guardia,Conserje")]
[Route("api/mobile")]
public class MobileApiController : Controller
{
    private readonly AppDbContext _context;

    public MobileApiController(AppDbContext context) => _context = context;

    private int GetEdificioId() =>
        int.TryParse(User.FindFirstValue("EdificioId"), out var id) ? id : 0;

    // GET /api/mobile/tareas
    [HttpGet("tareas")]
    public async Task<IActionResult> Tareas()
    {
        var edificioId = GetEdificioId();
        var tareas = await _context.Tareas
            .Include(t => t.Archivos)
            .Where(t => t.EdificioId == edificioId)
            .OrderBy(t => t.FechaCompletada != null && t.FechaCompletada.Value.Date == DateTime.Today)
            .ThenBy(t => t.HoraProgramada)
            .ThenBy(t => t.Titulo)
            .Select(t => new {
                t.Id,
                t.Titulo,
                t.Descripcion,
                HoraProgramada = t.HoraProgramada.HasValue
                    ? t.HoraProgramada.Value.ToString("HH:mm") : (string?)null,
                Turno = (int)t.Turno,
                FechaCompletada = t.FechaCompletada,
                ArchivosCount = t.Archivos.Count,
                CompletadaHoy = t.FechaCompletada != null
                    && t.FechaCompletada.Value.Date == DateTime.Today
            })
            .ToListAsync();

        return Json(new {
            ok = true,
            fecha = DateTime.Today.ToString("yyyy-MM-dd"),
            tareas
        });
    }

    // GET /api/mobile/tarea/{id}
    [HttpGet("tarea/{id}")]
    public async Task<IActionResult> DetalleTarea(int id)
    {
        var edificioId = GetEdificioId();
        var tarea = await _context.Tareas
            .Include(t => t.Edificio)
            .Include(t => t.Archivos)
            .Where(t => t.Id == id && t.EdificioId == edificioId)
            .Select(t => new {
                t.Id,
                t.Titulo,
                t.Descripcion,
                HoraProgramada = t.HoraProgramada.HasValue
                    ? t.HoraProgramada.Value.ToString("HH:mm") : (string?)null,
                Turno = (int)t.Turno,
                EdificioNombre = t.Edificio != null ? t.Edificio.Nombre : null,
                FechaCompletada = t.FechaCompletada,
                CompletadaHoy = t.FechaCompletada != null
                    && t.FechaCompletada.Value.Date == DateTime.Today,
                Archivos = t.Archivos.Select(a => new {
                    a.Id, a.NombreOriginal, a.NombreArchivo,
                    a.EsImagen, a.SubidoPor,
                    FechaSubida = a.FechaSubida.ToString("dd/MM/yyyy HH:mm")
                })
            })
            .FirstOrDefaultAsync();

        if (tarea == null) return NotFound();
        return Json(new { ok = true, tarea });
    }

    // GET /api/mobile/areas
    [HttpGet("areas")]
    public async Task<IActionResult> Areas()
    {
        var edificioId = GetEdificioId();
        var areas = await _context.Areas
            .Where(a => a.EdificioId == edificioId && a.Activo)
            .OrderBy(a => a.Orden)
            .Select(a => new { a.Id, a.Nombre, a.Descripcion, a.Orden })
            .ToListAsync();

        var edificio = await _context.Edificios
            .Where(e => e.Id == edificioId)
            .Select(e => new { e.Nombre })
            .FirstOrDefaultAsync();

        return Json(new { ok = true, areas, edificio });
    }

    // GET /api/mobile/ronda-activa
    [HttpGet("ronda-activa")]
    public async Task<IActionResult> RondaActiva()
    {
        var edificioId = GetEdificioId();
        var ronda = await _context.Rondas
            .Include(r => r.AreaRondas).ThenInclude(ar => ar.Area)
            .Where(r => r.EdificioId == edificioId && r.Estado == EstadoRonda.EnCurso)
            .FirstOrDefaultAsync();

        if (ronda == null) return Json(new { ok = true, ronda = (object?)null });

        return Json(new {
            ok = true,
            ronda = new {
                ronda.Id,
                FechaHora = ronda.FechaHora.ToString("HH:mm"),
                Areas = ronda.AreaRondas.Select(ar => new {
                    ar.Id,
                    ar.AreaId,
                    AreaNombre = ar.Area?.Nombre ?? "",
                    ar.Completada
                })
            }
        });
    }

    // GET /api/mobile/historial
    [HttpGet("historial")]
    public async Task<IActionResult> Historial()
    {
        var edificioId = GetEdificioId();
        var rondas = await _context.Rondas
            .Where(r => r.EdificioId == edificioId)
            .OrderByDescending(r => r.FechaHora)
            .Take(30)
            .Select(r => new {
                r.Id,
                r.NombreOperador,
                FechaHora = r.FechaHora.ToString("dd/MM/yyyy HH:mm"),
                Estado = r.Estado.ToString(),
                AreasCount = r.AreaRondas.Count,
                IncidenciasCount = _context.AreaRondas
                    .Where(ar => ar.RondaId == r.Id)
                    .SelectMany(ar => ar.Incidencias)
                    .Count()
            })
            .ToListAsync();

        return Json(new { ok = true, rondas });
    }
}
