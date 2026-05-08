using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Guardias.Data;
using Guardias.Models;

namespace Guardias.Controllers;

public class TareaController : Controller
{
    private readonly AppDbContext _context;

    public TareaController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /Tarea
    public async Task<IActionResult> Index()
    {
        var tareas = await _context.Tareas
            .Include(t => t.Guardia)
            .Include(t => t.Edificio)
            .Where(t => t.Estado != EstadoTarea.Completada)
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
        var tarea = await _context.Tareas.FindAsync(id);
        if (tarea == null) return NotFound();

        tarea.Estado = EstadoTarea.Completada;
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
