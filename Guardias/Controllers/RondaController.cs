using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Guardias.Data;
using Guardias.Models;
using Guardias.Services;

namespace Guardias.Controllers;

[Authorize(Roles = "Guardia")]
public class RondaController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly CloudinaryService _driveService;
    private static readonly string[] AllowedImageTypes = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxFileSize = 10 * 1024 * 1024;

    public RondaController(AppDbContext context, IWebHostEnvironment env, CloudinaryService driveService)
    {
        _context = context;
        _env = env;
        _driveService = driveService;
    }

    private int GetEdificioId() =>
        int.TryParse(User.FindFirstValue("EdificioId"), out var id) ? id : 0;

    // GET: /Ronda/Historial
    public async Task<IActionResult> Historial()
    {
        int edificioId = GetEdificioId();
        var rondas = await _context.Rondas
            .Include(r => r.Guardia)
            .Include(r => r.Edificio)
            .Include(r => r.AreaRondas).ThenInclude(ar => ar.Fotos)
            .Include(r => r.Incidencias)
            .Where(r => r.EdificioId == edificioId && r.Estado != EstadoRonda.ReporteDirecto)
            .OrderByDescending(r => r.FechaHora)
            .ToListAsync();

        return View(rondas);
    }

    // GET: /Ronda/Nueva
    public async Task<IActionResult> Nueva()
    {
        int edificioId = GetEdificioId();
        var guardias = await _context.Guardias
            .Where(g => g.Activo && g.EdificioId == edificioId)
            .OrderBy(g => g.Nombre)
            .ToListAsync();

        ViewBag.Guardias = guardias;
        return View();
    }

    // POST: /Ronda/Nueva
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Nueva(int? guardiaId)
    {
        int edificioId = GetEdificioId();

        Guardia? guardia = null;
        if (guardiaId.HasValue)
        {
            guardia = await _context.Guardias
                .FirstOrDefaultAsync(g => g.Id == guardiaId.Value && g.Activo && g.EdificioId == edificioId);

            if (guardia == null)
            {
                ModelState.AddModelError("", "Guardia no encontrado.");
                var lista = await _context.Guardias
                    .Where(g => g.Activo && g.EdificioId == edificioId)
                    .OrderBy(g => g.Nombre).ToListAsync();
                ViewBag.Guardias = lista;
                return View();
            }
        }

        // Redirect to existing in-progress ronda
        var existing = await _context.Rondas
            .Include(r => r.AreaRondas)
            .FirstOrDefaultAsync(r => r.EdificioId == edificioId && r.Estado == EstadoRonda.EnCurso);
        if (existing != null)
            return RedirectToAction("SeleccionArea", new { rondaId = existing.Id });

        var ronda = new Ronda
        {
            GuardiaId = guardia?.Id,
            NombreOperador = guardia?.Nombre ?? User.Identity?.Name,
            EdificioId = edificioId,
            FechaHora = DateTime.Now,
            Estado = EstadoRonda.EnCurso
        };

        _context.Rondas.Add(ronda);
        await _context.SaveChangesAsync();

        // Create AreaRonda entries for each active area of this building
        var areas = await _context.Areas
            .Where(a => a.EdificioId == edificioId && a.Activo)
            .OrderBy(a => a.Orden)
            .ToListAsync();

        foreach (var area in areas)
        {
            _context.AreaRondas.Add(new AreaRonda
            {
                RondaId = ronda.Id,
                AreaId = area.Id,
                Completada = false
            });
        }
        await _context.SaveChangesAsync();

        return RedirectToAction("SeleccionArea", new { rondaId = ronda.Id });
    }

    // GET: /Ronda/SeleccionArea?rondaId=5
    public async Task<IActionResult> SeleccionArea(int rondaId)
    {
        int edificioId = GetEdificioId();
        var ronda = await _context.Rondas
            .Include(r => r.Guardia)
            .Include(r => r.Edificio)
            .Include(r => r.AreaRondas).ThenInclude(ar => ar.Area)
            .Include(r => r.AreaRondas).ThenInclude(ar => ar.Fotos)
            .FirstOrDefaultAsync(r => r.Id == rondaId && r.EdificioId == edificioId);

        if (ronda == null) return NotFound();
        if (ronda.Estado != EstadoRonda.EnCurso) return RedirectToAction("Historial");
        return View(ronda);
    }

    // GET: /Ronda/CheckArea?rondaId=5&areaRondaId=12
    public async Task<IActionResult> CheckArea(int rondaId, int areaRondaId)
    {
        int edificioId = GetEdificioId();
        var ronda = await _context.Rondas
            .Include(r => r.Guardia)
            .Include(r => r.Edificio)
            .Include(r => r.AreaRondas).ThenInclude(ar => ar.Area)
            .Include(r => r.AreaRondas).ThenInclude(ar => ar.Fotos)
            .FirstOrDefaultAsync(r => r.Id == rondaId && r.EdificioId == edificioId);

        if (ronda == null) return NotFound();
        if (ronda.Estado != EstadoRonda.EnCurso) return RedirectToAction("Historial");

        var current = ronda.AreaRondas.FirstOrDefault(ar => ar.Id == areaRondaId);
        if (current == null) return RedirectToAction("SeleccionArea", new { rondaId });

        ViewBag.RondaId = rondaId;
        ViewBag.AreaRondaId = current.Id;
        ViewBag.AreaNombre = current.Area?.Nombre;
        ViewBag.AreaDescripcion = current.Area?.Descripcion;
        ViewBag.Total = ronda.AreaRondas.Count;
        ViewBag.CompletedCount = ronda.AreaRondas.Count(ar => ar.Completada);
        return View(ronda);
    }

    // POST: /Ronda/CheckArea
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> CheckArea(int rondaId, int areaRondaId, List<IFormFile> fotos, string? notas,
        string? incDescripcion, SeveridadIncidencia incSeveridad = SeveridadIncidencia.Leve)
    {
        int edificioId = GetEdificioId();
        var ronda = await _context.Rondas
            .Include(r => r.AreaRondas).ThenInclude(ar => ar.Area)
            .FirstOrDefaultAsync(r => r.Id == rondaId && r.EdificioId == edificioId);

        if (ronda == null) return NotFound();

        var areaRonda = await _context.AreaRondas.FindAsync(areaRondaId);
        if (areaRonda == null || areaRonda.RondaId != rondaId) return NotFound();

        var validFotos = fotos?.Where(f => f.Length > 0).ToList() ?? new List<IFormFile>();
        if (!validFotos.Any())
        {
            ModelState.AddModelError("fotos", "Debe subir al menos una foto del área.");
            var rondaReload = await _context.Rondas
                .Include(r => r.Guardia)
                .Include(r => r.Edificio)
                .Include(r => r.AreaRondas).ThenInclude(ar => ar.Area)
                .Include(r => r.AreaRondas).ThenInclude(ar => ar.Fotos)
                .FirstOrDefaultAsync(r => r.Id == rondaId);
            var curReload = rondaReload!.AreaRondas.FirstOrDefault(ar => ar.Id == areaRondaId);
            if (curReload == null) return RedirectToAction("SeleccionArea", new { rondaId });
            ViewBag.RondaId = rondaId;
            ViewBag.AreaRondaId = curReload.Id;
            ViewBag.AreaNombre = curReload.Area?.Nombre;
            ViewBag.AreaDescripcion = curReload.Area?.Descripcion;
            ViewBag.Total = rondaReload.AreaRondas.Count;
            ViewBag.CompletedCount = rondaReload.AreaRondas.Count(ar => ar.Completada);
            return View(rondaReload);
        }

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "fotos");

        foreach (var foto in validFotos)
        {
            var ext = Path.GetExtension(foto.FileName).ToLowerInvariant();
            if (!AllowedImageTypes.Contains(ext) || foto.Length > MaxFileSize) continue;

            string rutaFoto;
            string? driveFileId = null;

            if (_driveService.IsConfigured)
            {
                // Subir a Google Drive
                var fileName = Guid.NewGuid().ToString("N") + ext;
                await using var ms = new MemoryStream();
                await foto.CopyToAsync(ms);
                ms.Position = 0;
                var (fileId, url) = await _driveService.UploadAsync(ms, fileName, foto.ContentType);
                rutaFoto = url;
                driveFileId = fileId;
            }
            else
            {
                // Fallback: guardar localmente
                Directory.CreateDirectory(uploadsDir);
                var fileName = Guid.NewGuid().ToString("N") + ext;
                var filePath = Path.Combine(uploadsDir, fileName);
                await using var stream = new FileStream(filePath, FileMode.Create);
                await foto.CopyToAsync(stream);
                rutaFoto = $"/uploads/fotos/{fileName}";
            }

            _context.FotosRonda.Add(new FotoRonda
            {
                AreaRondaId = areaRondaId,
                RutaFoto = rutaFoto,
                DriveFileId = driveFileId,
                FechaCaptura = DateTime.Now
            });
        }

        areaRonda.Completada = true;
        areaRonda.Notas = notas;
        areaRonda.FechaCompletada = DateTime.Now;

        if (!string.IsNullOrWhiteSpace(incDescripcion))
        {
            _context.Incidencias.Add(new Incidencia
            {
                RondaId = rondaId,
                AreaRondaId = areaRondaId,
                Descripcion = incDescripcion.Trim(),
                Severidad = incSeveridad,
                Estado = EstadoIncidencia.Abierta,
                FechaCreacion = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("SeleccionArea", new { rondaId });
    }

    // GET: /Ronda/FinalizarRonda?rondaId=5
    public async Task<IActionResult> FinalizarRonda(int rondaId)
    {
        int edificioId = GetEdificioId();
        var ronda = await _context.Rondas
            .Include(r => r.Guardia)
            .Include(r => r.Edificio)
            .Include(r => r.AreaRondas).ThenInclude(ar => ar.Area)
            .Include(r => r.AreaRondas).ThenInclude(ar => ar.Fotos)
            .Include(r => r.Incidencias)
            .FirstOrDefaultAsync(r => r.Id == rondaId && r.EdificioId == edificioId);

        if (ronda == null) return NotFound();
        if (ronda.Estado != EstadoRonda.EnCurso) return RedirectToAction("Historial");
        return View(ronda);
    }

    // POST: /Ronda/FinalizarRonda
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FinalizarRonda(int rondaId, string? reporteIncidencias, SeveridadIncidencia severidad = SeveridadIncidencia.Leve)
    {
        int edificioId = GetEdificioId();
        var ronda = await _context.Rondas
            .Include(r => r.AreaRondas)
            .FirstOrDefaultAsync(r => r.Id == rondaId && r.EdificioId == edificioId);

        if (ronda == null) return NotFound();

        // No permitir finalizar si quedan áreas sin revisar
        if (ronda.AreaRondas.Any(ar => !ar.Completada))
        {
            TempData["Error"] = "Debes revisar todas las áreas antes de finalizar la ronda.";
            return RedirectToAction("SeleccionArea", new { rondaId });
        }

        ronda.Estado = EstadoRonda.Finalizada;
        ronda.ReporteIncidencias = reporteIncidencias;

        if (!string.IsNullOrWhiteSpace(reporteIncidencias))
        {
            _context.Incidencias.Add(new Incidencia
            {
                RondaId = rondaId,
                Descripcion = reporteIncidencias,
                Severidad = severidad,
                Estado = EstadoIncidencia.Abierta,
                FechaCreacion = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Ronda finalizada correctamente.";
        return RedirectToAction("Historial");
    }

    // GET: /Ronda/Detalle/5
    public async Task<IActionResult> Detalle(int id)
    {
        int edificioId = GetEdificioId();
        var ronda = await _context.Rondas
            .Include(r => r.Guardia)
            .Include(r => r.Edificio)
            .Include(r => r.AreaRondas).ThenInclude(ar => ar.Area)
            .Include(r => r.AreaRondas).ThenInclude(ar => ar.Fotos)
            .Include(r => r.Incidencias)
            .FirstOrDefaultAsync(r => r.Id == id && r.EdificioId == edificioId);

        if (ronda == null) return NotFound();
        return View(ronda);
    }
}

