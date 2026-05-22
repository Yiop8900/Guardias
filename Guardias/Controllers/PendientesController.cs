using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Guardias.Data;
using Guardias.Models;

namespace Guardias.Controllers;

[Authorize(Roles = "Guardia")]
public class PendientesController : Controller
{
    private readonly AppDbContext _context;

    public PendientesController(AppDbContext context)
    {
        _context = context;
    }

    private int GetEdificioId() =>
        int.TryParse(User.FindFirstValue("EdificioId"), out var id) ? id : 0;

    // GET: /Pendientes
    public async Task<IActionResult> Index()
    {
        int edificioId = GetEdificioId();

        var rondasSinFirmar = await _context.Rondas
            .Include(r => r.Guardia)
            .Include(r => r.Edificio)
            .Where(r => r.Estado == EstadoRonda.Finalizada && r.EdificioId == edificioId)
            .OrderByDescending(r => r.FechaHora)
            .ToListAsync();

        var incidenciasGraves = await _context.Incidencias
            .Include(i => i.Ronda)
                .ThenInclude(r => r!.Guardia)
            .Where(i => i.Severidad == SeveridadIncidencia.Grave &&
                        i.Estado != EstadoIncidencia.Cerrada &&
                        i.Ronda!.EdificioId == edificioId)
            .OrderByDescending(i => i.FechaCreacion)
            .ToListAsync();

        ViewBag.RondasSinFirmar = rondasSinFirmar;
        ViewBag.IncidenciasGraves = incidenciasGraves;
        ViewBag.Total = rondasSinFirmar.Count + incidenciasGraves.Count;

        return View();
    }
}
