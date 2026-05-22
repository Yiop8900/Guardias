using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Guardias.Data;
using Guardias.Models;

namespace Guardias.Controllers;

[Authorize(Roles = "Guardia")]
public class TareaController : Controller
{
    private readonly AppDbContext _context;

    public TareaController(AppDbContext context)
    {
        _context = context;
    }

    private int GetEdificioId() =>
        int.TryParse(User.FindFirstValue("EdificioId"), out var id) ? id : 0;

    // GET: /Tarea
    public async Task<IActionResult> Index()
    {
        int edificioId = GetEdificioId();
        var tareas = await _context.Tareas
            .Include(t => t.Guardia)
            .Include(t => t.Edificio)
            .Where(t => t.Estado != EstadoTarea.Completada && t.EdificioId == edificioId)
            .OrderBy(t => t.HoraProgramada)
            .ThenBy(t => t.Titulo)
            .ToListAsync();

        ViewBag.TotalPendientes = tareas.Count(t => t.Estado == EstadoTarea.Pendiente);
        return View(tareas);
    }

    // POST: /Tarea/Completar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Completar(int id)
    {
        int edificioId = GetEdificioId();
        var tarea = await _context.Tareas.FirstOrDefaultAsync(t => t.Id == id && t.EdificioId == edificioId);
        if (tarea == null) return NotFound();

        tarea.Estado = EstadoTarea.Completada;
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
