using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Guardias.Data;
using Guardias.Models;

namespace Guardias.Controllers;

public class IncidenciaController : Controller
{
    private readonly AppDbContext _context;

    public IncidenciaController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /Incidencia
    public async Task<IActionResult> Index(string? filtro)
    {
        var query = _context.Incidencias
            .Include(i => i.Ronda)
                .ThenInclude(r => r!.Guardia)
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
            .CountAsync(i => i.Severidad >= SeveridadIncidencia.Moderada && i.Estado != EstadoIncidencia.Cerrada);

        return View(incidencias);
    }

    // GET: /Incidencia/Detalle/5
    public async Task<IActionResult> Detalle(int id)
    {
        var incidencia = await _context.Incidencias
            .Include(i => i.Ronda)
                .ThenInclude(r => r!.Guardia)
            .Include(i => i.Ronda)
                .ThenInclude(r => r!.Edificio)
            .Include(i => i.Ronda)
                .ThenInclude(r => r!.Fotos)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (incidencia == null) return NotFound();
        return View(incidencia);
    }
}
