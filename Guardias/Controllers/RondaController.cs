using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Guardias.Data;
using Guardias.Models;

namespace Guardias.Controllers;

public class RondaController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private static readonly string[] AllowedImageTypes = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public RondaController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // GET: /Ronda/Historial
    public async Task<IActionResult> Historial()
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

    // GET: /Ronda/Nueva
    public async Task<IActionResult> Nueva()
    {
        var guardias = await _context.Guardias
            .Where(g => g.Activo)
            .Include(g => g.Edificio)
            .OrderBy(g => g.Nombre)
            .ToListAsync();

        ViewBag.Guardias = guardias;
        return View();
    }

    // POST: /Ronda/Nueva
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Nueva(int guardiaId)
    {
        var guardia = await _context.Guardias
            .Include(g => g.Edificio)
            .FirstOrDefaultAsync(g => g.Id == guardiaId && g.Activo);

        if (guardia == null)
        {
            ModelState.AddModelError("", "Guardia no encontrado o inactivo.");
            var guardias = await _context.Guardias
                .Where(g => g.Activo)
                .Include(g => g.Edificio)
                .OrderBy(g => g.Nombre)
                .ToListAsync();
            ViewBag.Guardias = guardias;
            return View();
        }

        var ronda = new Ronda
        {
            GuardiaId = guardia.Id,
            EdificioId = guardia.EdificioId,
            FechaHora = DateTime.Now,
            Estado = EstadoRonda.EnCurso
        };

        _context.Rondas.Add(ronda);
        await _context.SaveChangesAsync();

        return RedirectToAction("NuevaFotos", new { id = ronda.Id });
    }

    // GET: /Ronda/NuevaFotos/5
    public async Task<IActionResult> NuevaFotos(int id)
    {
        var ronda = await _context.Rondas
            .Include(r => r.Guardia)
            .Include(r => r.Edificio)
            .Include(r => r.Fotos)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (ronda == null) return NotFound();
        if (ronda.Estado != EstadoRonda.EnCurso) return RedirectToAction("Historial");

        return View(ronda);
    }

    // POST: /Ronda/NuevaFotos/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52_428_800)] // 50 MB total
    public async Task<IActionResult> NuevaFotos(int id, List<IFormFile> fotos, string? reporteIncidencias, SeveridadIncidencia severidad = SeveridadIncidencia.Leve)
    {
        var ronda = await _context.Rondas
            .Include(r => r.Guardia)
            .Include(r => r.Edificio)
            .Include(r => r.Fotos)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (ronda == null) return NotFound();

        var validFotos = fotos?.Where(f => f.Length > 0).ToList() ?? new List<IFormFile>();
        if (!validFotos.Any())
        {
            ModelState.AddModelError("fotos", "Debe subir al menos una foto para finalizar la ronda.");
            return View(ronda);
        }

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "fotos");
        Directory.CreateDirectory(uploadsDir);

        foreach (var foto in validFotos)
        {
            var ext = Path.GetExtension(foto.FileName).ToLowerInvariant();
            if (!AllowedImageTypes.Contains(ext) || foto.Length > MaxFileSize)
                continue;

            var fileName = Guid.NewGuid().ToString("N") + ext;
            var filePath = Path.Combine(uploadsDir, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await foto.CopyToAsync(stream);

            _context.FotosRonda.Add(new FotoRonda
            {
                RondaId = id,
                RutaFoto = $"/uploads/fotos/{fileName}",
                FechaCaptura = DateTime.Now
            });
        }

        ronda.Estado = EstadoRonda.Finalizada;
        ronda.ReporteIncidencias = reporteIncidencias;

        if (!string.IsNullOrWhiteSpace(reporteIncidencias))
        {
            _context.Incidencias.Add(new Incidencia
            {
                RondaId = id,
                Descripcion = reporteIncidencias,
                Severidad = severidad,
                Estado = EstadoIncidencia.Abierta,
                FechaCreacion = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Historial");
    }

    // GET: /Ronda/Detalle/5
    public async Task<IActionResult> Detalle(int id)
    {
        var ronda = await _context.Rondas
            .Include(r => r.Guardia)
            .Include(r => r.Edificio)
            .Include(r => r.Fotos)
            .Include(r => r.Incidencias)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (ronda == null) return NotFound();
        return View(ronda);
    }
}
