using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Guardias.Data;
using Guardias.Models;

namespace Guardias.Controllers;

[Authorize(Roles = "Guardia,Conserje")]
public class TareaController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public TareaController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
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
            .Include(t => t.Archivos)
            .Where(t => t.EdificioId == edificioId)
            .OrderBy(t => t.FechaCompletada != null && t.FechaCompletada.Value.Date == DateTime.Today) // completadas al final
            .ThenBy(t => t.HoraProgramada)
            .ThenBy(t => t.Titulo)
            .ToListAsync();

        ViewBag.TotalPendientes = tareas.Count(t => !t.CompletadaHoy);
        return View(tareas);
    }

    // GET: /Tarea/Detalle/5
    public async Task<IActionResult> Detalle(int id)
    {
        int edificioId = GetEdificioId();
        var tarea = await _context.Tareas
            .Include(t => t.Guardia)
            .Include(t => t.Edificio)
            .Include(t => t.Archivos)
            .FirstOrDefaultAsync(t => t.Id == id && t.EdificioId == edificioId);

        if (tarea == null) return NotFound();
        return View(tarea);
    }

    // POST: /Tarea/SubirArchivos/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirArchivos(int id, List<IFormFile> archivos)
    {
        int edificioId = GetEdificioId();
        var tarea = await _context.Tareas
            .FirstOrDefaultAsync(t => t.Id == id && t.EdificioId == edificioId);
        if (tarea == null) return NotFound();

        if (archivos == null || archivos.Count == 0)
        {
            TempData["Error"] = "No se seleccionaron archivos.";
            return RedirectToAction("Detalle", new { id });
        }

        var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "tareas");
        Directory.CreateDirectory(uploadPath);

        var extensionesPermitidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".heic",
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt"
        };

        foreach (var file in archivos)
        {
            if (file.Length == 0) continue;

            var ext = Path.GetExtension(file.FileName);
            if (!extensionesPermitidas.Contains(ext))
            {
                TempData["Error"] = $"Tipo de archivo no permitido: {ext}";
                continue;
            }

            var nombreGuardado = $"{Guid.NewGuid()}{ext}";
            var rutaCompleta = Path.Combine(uploadPath, nombreGuardado);

            using var stream = new FileStream(rutaCompleta, FileMode.Create);
            await file.CopyToAsync(stream);

            var esImagen = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".heic" }
                .Contains(ext, StringComparer.OrdinalIgnoreCase);

            _context.TareaArchivos.Add(new TareaArchivo
            {
                TareaId = id,
                NombreOriginal = file.FileName,
                NombreArchivo = nombreGuardado,
                TipoMime = file.ContentType,
                EsImagen = esImagen,
                SubidoPor = User.Identity!.Name ?? ""
            });
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Archivo(s) subido(s) correctamente.";
        return RedirectToAction("Detalle", new { id });
    }

    // POST: /Tarea/EliminarArchivo/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarArchivo(int archivoId, int tareaId)
    {
        int edificioId = GetEdificioId();
        var archivo = await _context.TareaArchivos
            .Include(a => a.Tarea)
            .FirstOrDefaultAsync(a => a.Id == archivoId && a.Tarea!.EdificioId == edificioId);

        if (archivo != null)
        {
            var ruta = Path.Combine(_env.WebRootPath, "uploads", "tareas", archivo.NombreArchivo);
            if (System.IO.File.Exists(ruta))
                System.IO.File.Delete(ruta);

            _context.TareaArchivos.Remove(archivo);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Detalle", new { id = tareaId });
    }

    // POST: /Tarea/Completar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Completar(int id)
    {
        int edificioId = GetEdificioId();
        var tarea = await _context.Tareas
            .Include(t => t.Archivos)
            .FirstOrDefaultAsync(t => t.Id == id && t.EdificioId == edificioId);
        if (tarea == null) return NotFound();

        if (!tarea.Archivos.Any())
        {
            TempData["Error"] = "Debes adjuntar al menos un archivo antes de completar la tarea.";
            return RedirectToAction("Detalle", new { id });
        }

        tarea.FechaCompletada = DateTime.Now;
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
