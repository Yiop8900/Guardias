using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Guardias.Data;
using Guardias.Models;

namespace Guardias.Controllers;

[Authorize(Roles = "Guardia,Admin")]
public class IncidenciaController : Controller
{
    private readonly AppDbContext _context;

    public IncidenciaController(AppDbContext context)
    {
        _context = context;
    }

    private int GetEdificioId() =>
        int.TryParse(User.FindFirstValue("EdificioId"), out var id) ? id : 0;

    // GET: /Incidencia
    public async Task<IActionResult> Index(string? filtro)
    {
        int edificioId = GetEdificioId();
        var query = _context.Incidencias
            .Include(i => i.Ronda)
                .ThenInclude(r => r!.Guardia)
            .Where(i => i.Ronda!.EdificioId == edificioId)
            .AsQueryable();

        query = filtro switch
        {
            "abiertas" => query.Where(i => i.Estado == EstadoIncidencia.Abierta),
            "enproceso" => query.Where(i => i.Estado == EstadoIncidencia.EnProceso),
            "cerradas" => query.Where(i => i.Estado == EstadoIncidencia.Cerrada),
            "graves" => query.Where(i => i.Severidad == SeveridadIncidencia.Grave),
            _ => query
        };

        var incidencias = await query.OrderByDescending(i => i.FechaCreacion).ToListAsync();
        ViewBag.Filtro = filtro ?? "todas";
        ViewBag.TotalModeradasGraves = await _context.Incidencias
            .CountAsync(i => i.Ronda!.EdificioId == edificioId &&
                             i.Severidad >= SeveridadIncidencia.Moderada &&
                             i.Estado != EstadoIncidencia.Cerrada);

        return View(incidencias);
    }

    // GET: /Incidencia/Detalle/5
    public async Task<IActionResult> Detalle(int id)
    {
        int edificioId = GetEdificioId();
        var incidencia = await _context.Incidencias
            .Include(i => i.Ronda)
                .ThenInclude(r => r!.Guardia)
            .Include(i => i.Ronda)
                .ThenInclude(r => r!.Edificio)
            .Include(i => i.Ronda)
                .ThenInclude(r => r!.AreaRondas)
                    .ThenInclude(ar => ar.Fotos)
            .FirstOrDefaultAsync(i => i.Id == id && i.Ronda!.EdificioId == edificioId);

        if (incidencia == null) return NotFound();
        return View(incidencia);
    }

    // GET: /Incidencia/ExportarPdf/5
    [Authorize(Roles = "Guardia,Admin")]
    public async Task<IActionResult> ExportarPdf(int id)
    {
        IQueryable<Incidencia> query = _context.Incidencias
            .Include(i => i.Ronda).ThenInclude(r => r!.Guardia)
            .Include(i => i.Ronda).ThenInclude(r => r!.Edificio)
            .Include(i => i.Ronda).ThenInclude(r => r!.AreaRondas).ThenInclude(ar => ar.Area)
            .Include(i => i.Ronda).ThenInclude(r => r!.AreaRondas).ThenInclude(ar => ar.Fotos);

        Incidencia? incidencia;
        if (User.IsInRole("Admin"))
        {
            incidencia = await query.FirstOrDefaultAsync(i => i.Id == id);
        }
        else
        {
            int edificioId = GetEdificioId();
            incidencia = await query.FirstOrDefaultAsync(i => i.Id == id && i.Ronda!.EdificioId == edificioId);
        }

        if (incidencia == null) return NotFound();
        return View(incidencia);
    }
}
