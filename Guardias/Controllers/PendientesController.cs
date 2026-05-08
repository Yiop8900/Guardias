using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Guardias.Data;
using Guardias.Models;

namespace Guardias.Controllers;

public class PendientesController : Controller
{
    private readonly AppDbContext _context;

    public PendientesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /Pendientes
    public async Task<IActionResult> Index()
    {
        var rondasSinFirmar = await _context.Rondas
            .Include(r => r.Guardia)
            .Include(r => r.Edificio)
            .Where(r => r.Estado == EstadoRonda.Finalizada)
            .OrderByDescending(r => r.FechaHora)
            .ToListAsync();

        var incidenciasGraves = await _context.Incidencias
            .Include(i => i.Ronda)
                .ThenInclude(r => r!.Guardia)
            .Where(i => i.Severidad == SeveridadIncidencia.Grave && i.Estado != EstadoIncidencia.Cerrada)
            .OrderByDescending(i => i.FechaCreacion)
            .ToListAsync();

        ViewBag.RondasSinFirmar = rondasSinFirmar;
        ViewBag.IncidenciasGraves = incidenciasGraves;
        ViewBag.Total = rondasSinFirmar.Count + incidenciasGraves.Count;

        return View();
    }
}
