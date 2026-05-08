using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Guardias.Data;
using Guardias.Models;

namespace Guardias.Controllers;

public class AdminController : Controller
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    // ===== DASHBOARD =====
    public async Task<IActionResult> Index()
    {
        ViewBag.TotalGuardias = await _context.Guardias.CountAsync(g => g.Activo);
        ViewBag.TotalEdificios = await _context.Edificios.CountAsync(e => e.Activo);
        ViewBag.TotalRondas = await _context.Rondas.CountAsync();
        ViewBag.RondasHoy = await _context.Rondas.CountAsync(r => r.FechaHora.Date == DateTime.Today);
        ViewBag.IncidenciasAbiertas = await _context.Incidencias.CountAsync(i => i.Estado == EstadoIncidencia.Abierta);
        ViewBag.TareasPendientes = await _context.Tareas.CountAsync(t => t.Estado == EstadoTarea.Pendiente);
        ViewBag.RondasPendientesFirma = await _context.Rondas.CountAsync(r => r.Estado == EstadoRonda.Finalizada);
        return View();
    }

    // ===== GUARDIAS =====
    public async Task<IActionResult> Guardias()
    {
        var guardias = await _context.Guardias
            .Include(g => g.Edificio)
            .OrderBy(g => g.Nombre)
            .ToListAsync();
        return View(guardias);
    }

    public async Task<IActionResult> CrearGuardia()
    {
        await LoadEdificiosViewBag();
        return View(new Guardia());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearGuardia(Guardia guardia)
    {
        if (ModelState.IsValid)
        {
            _context.Guardias.Add(guardia);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Guardia creado correctamente.";
            return RedirectToAction("Guardias");
        }
        await LoadEdificiosViewBag();
        return View(guardia);
    }

    public async Task<IActionResult> EditarGuardia(int id)
    {
        var guardia = await _context.Guardias.FindAsync(id);
        if (guardia == null) return NotFound();
        await LoadEdificiosViewBag();
        return View(guardia);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarGuardia(int id, Guardia guardia)
    {
        if (id != guardia.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            _context.Update(guardia);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Guardia actualizado correctamente.";
            return RedirectToAction("Guardias");
        }
        await LoadEdificiosViewBag();
        return View(guardia);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarGuardia(int id)
    {
        var guardia = await _context.Guardias.FindAsync(id);
        if (guardia != null)
        {
            guardia.Activo = false;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Guardia desactivado.";
        }
        return RedirectToAction("Guardias");
    }

    // ===== EDIFICIOS =====
    public async Task<IActionResult> Edificios()
    {
        var edificios = await _context.Edificios.OrderBy(e => e.Nombre).ToListAsync();
        return View(edificios);
    }

    public IActionResult CrearEdificio() => View(new Edificio());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearEdificio(Edificio edificio)
    {
        if (ModelState.IsValid)
        {
            _context.Edificios.Add(edificio);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Edificio creado correctamente.";
            return RedirectToAction("Edificios");
        }
        return View(edificio);
    }

    public async Task<IActionResult> EditarEdificio(int id)
    {
        var edificio = await _context.Edificios.FindAsync(id);
        if (edificio == null) return NotFound();
        return View(edificio);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarEdificio(int id, Edificio edificio)
    {
        if (id != edificio.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            _context.Update(edificio);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Edificio actualizado.";
            return RedirectToAction("Edificios");
        }
        return View(edificio);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarEdificio(int id)
    {
        var edificio = await _context.Edificios.FindAsync(id);
        if (edificio != null)
        {
            edificio.Activo = false;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Edificio desactivado.";
        }
        return RedirectToAction("Edificios");
    }

    // ===== TAREAS =====
    public async Task<IActionResult> Tareas()
    {
        var tareas = await _context.Tareas
            .Include(t => t.Guardia)
            .Include(t => t.Edificio)
            .OrderBy(t => t.HoraProgramada)
            .ThenBy(t => t.Titulo)
            .ToListAsync();
        return View(tareas);
    }

    public async Task<IActionResult> CrearTarea()
    {
        await LoadGuardiasViewBag();
        await LoadEdificiosViewBag();
        return View(new Tarea());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearTarea(Tarea tarea)
    {
        if (ModelState.IsValid)
        {
            tarea.FechaCreacion = DateTime.Now;
            _context.Tareas.Add(tarea);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tarea creada correctamente.";
            return RedirectToAction("Tareas");
        }
        await LoadGuardiasViewBag();
        await LoadEdificiosViewBag();
        return View(tarea);
    }

    public async Task<IActionResult> EditarTarea(int id)
    {
        var tarea = await _context.Tareas.FindAsync(id);
        if (tarea == null) return NotFound();
        await LoadGuardiasViewBag();
        await LoadEdificiosViewBag();
        return View(tarea);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarTarea(int id, Tarea tarea)
    {
        if (id != tarea.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            _context.Update(tarea);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tarea actualizada.";
            return RedirectToAction("Tareas");
        }
        await LoadGuardiasViewBag();
        await LoadEdificiosViewBag();
        return View(tarea);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarTarea(int id)
    {
        var tarea = await _context.Tareas.FindAsync(id);
        if (tarea != null)
        {
            _context.Tareas.Remove(tarea);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tarea eliminada.";
        }
        return RedirectToAction("Tareas");
    }

    // ===== RONDAS =====
    public async Task<IActionResult> Rondas()
    {
        var rondas = await _context.Rondas
            .Include(r => r.Guardia)
            .Include(r => r.Edificio)
            .Include(r => r.Fotos)
            .Include(r => r.Incidencias)
            .OrderByDescending(r => r.FechaHora)
            .ToListAsync();
        return View(rondas);
    }

    public async Task<IActionResult> FirmarRonda(int id)
    {
        var ronda = await _context.Rondas
            .Include(r => r.Guardia)
            .Include(r => r.Edificio)
            .Include(r => r.Fotos)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (ronda == null) return NotFound();
        return View(ronda);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FirmarRonda(int id, string firmadoPor)
    {
        var ronda = await _context.Rondas.FindAsync(id);
        if (ronda == null) return NotFound();

        if (string.IsNullOrWhiteSpace(firmadoPor))
        {
            ModelState.AddModelError("firmadoPor", "El nombre del supervisor es obligatorio.");
            var rondaFull = await _context.Rondas
                .Include(r => r.Guardia)
                .Include(r => r.Edificio)
                .Include(r => r.Fotos)
                .FirstOrDefaultAsync(r => r.Id == id);
            return View(rondaFull);
        }

        ronda.Estado = EstadoRonda.FirmadaSupervisor;
        ronda.FirmadoPor = firmadoPor.Trim();
        ronda.FechaFirma = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Ronda firmada correctamente.";
        return RedirectToAction("Rondas");
    }

    // ===== INCIDENCIAS =====
    public async Task<IActionResult> Incidencias()
    {
        var incidencias = await _context.Incidencias
            .Include(i => i.Ronda)
                .ThenInclude(r => r!.Guardia)
            .Include(i => i.Ronda)
                .ThenInclude(r => r!.Edificio)
            .OrderByDescending(i => i.FechaCreacion)
            .ToListAsync();
        return View(incidencias);
    }

    public async Task<IActionResult> GestionarIncidencia(int id)
    {
        var incidencia = await _context.Incidencias
            .Include(i => i.Ronda)
                .ThenInclude(r => r!.Guardia)
            .Include(i => i.Ronda)
                .ThenInclude(r => r!.Edificio)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (incidencia == null) return NotFound();
        return View(incidencia);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GestionarIncidencia(int id, EstadoIncidencia estado, SeveridadIncidencia severidad, string? notasCierre)
    {
        var incidencia = await _context.Incidencias.FindAsync(id);
        if (incidencia == null) return NotFound();

        incidencia.Estado = estado;
        incidencia.Severidad = severidad;
        incidencia.NotasCierre = notasCierre;

        if (estado == EstadoIncidencia.Cerrada && incidencia.FechaCierre == null)
            incidencia.FechaCierre = DateTime.Now;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Incidencia actualizada.";
        return RedirectToAction("Incidencias");
    }

    // ===== HELPERS =====
    private async Task LoadEdificiosViewBag()
    {
        var edificios = await _context.Edificios
            .Where(e => e.Activo)
            .OrderBy(e => e.Nombre)
            .ToListAsync();
        ViewBag.Edificios = new SelectList(edificios, "Id", "Nombre");
    }

    private async Task LoadGuardiasViewBag()
    {
        var guardias = await _context.Guardias
            .Where(g => g.Activo)
            .OrderBy(g => g.Nombre)
            .ToListAsync();
        ViewBag.GuardiasList = new SelectList(guardias, "Id", "Nombre");
    }
}
