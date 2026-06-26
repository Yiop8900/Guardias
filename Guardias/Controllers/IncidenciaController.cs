using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Guardias.Data;
using Guardias.Models;
using Guardias.Services;

namespace Guardias.Controllers;

[Authorize(Roles = "Guardia,Conserje,Admin")]
public class IncidenciaController : Controller
{
    private readonly AppDbContext _context;
    private readonly CloudinaryService _cloudinary;
    private readonly IWebHostEnvironment _env;
    private static readonly string[] AllowedImageTypes = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxFileSize = 10 * 1024 * 1024;

    public IncidenciaController(AppDbContext context, CloudinaryService cloudinary, IWebHostEnvironment env)
    {
        _context = context;
        _cloudinary = cloudinary;
        _env = env;
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
            .Include(i => i.Ronda)
                .ThenInclude(r => r!.Edificio)
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

    // GET: /Incidencia/Reportar
    [Authorize(Roles = "Guardia,Conserje")]
    public async Task<IActionResult> Reportar()
    {
        int edificioId = GetEdificioId();
        var areas = await _context.Areas
            .Where(a => a.EdificioId == edificioId && a.Activo)
            .OrderBy(a => a.Orden)
            .ToListAsync();
        ViewBag.Areas = areas;
        return View();
    }

    // POST: /Incidencia/Reportar
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52_428_800)]
    [Authorize(Roles = "Guardia,Conserje")]
    public async Task<IActionResult> Reportar(int areaId, string descripcion,
        SeveridadIncidencia severidad, List<IFormFile> fotos)
    {
        int edificioId = GetEdificioId();

        var area = await _context.Areas
            .FirstOrDefaultAsync(a => a.Id == areaId && a.EdificioId == edificioId);

        if (area == null || string.IsNullOrWhiteSpace(descripcion))
        {
            ModelState.AddModelError("", area == null ? "Área no encontrada." : "La descripción es obligatoria.");
            var areasList = await _context.Areas
                .Where(a => a.EdificioId == edificioId && a.Activo)
                .OrderBy(a => a.Orden).ToListAsync();
            ViewBag.Areas = areasList;
            return View();
        }

        var validFotos = fotos?.Where(f => f.Length > 0).ToList() ?? new List<IFormFile>();
        if (!validFotos.Any())
        {
            ModelState.AddModelError("fotos", "Debe subir al menos una fotografía.");
            var areasList = await _context.Areas
                .Where(a => a.EdificioId == edificioId && a.Activo)
                .OrderBy(a => a.Orden).ToListAsync();
            ViewBag.Areas = areasList;
            return View();
        }

        // Crear ronda de reporte directo
        var ronda = new Ronda
        {
            EdificioId = edificioId,
            NombreOperador = User.Identity?.Name,
            FechaHora = DateTime.Now,
            Estado = EstadoRonda.ReporteDirecto
        };
        _context.Rondas.Add(ronda);
        await _context.SaveChangesAsync();

        // Crear AreaRonda para el área seleccionada
        var areaRonda = new AreaRonda
        {
            RondaId = ronda.Id,
            AreaId = area.Id,
            Completada = true,
            FechaCompletada = DateTime.Now
        };
        _context.AreaRondas.Add(areaRonda);
        await _context.SaveChangesAsync();

        // Subir fotos
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "fotos");
        foreach (var foto in validFotos)
        {
            var ext = Path.GetExtension(foto.FileName).ToLowerInvariant();
            if (!AllowedImageTypes.Contains(ext) || foto.Length > MaxFileSize) continue;

            string rutaFoto;
            string? fileId = null;

            if (_cloudinary.IsConfigured)
            {
                var fileName = Guid.NewGuid().ToString("N") + ext;
                await using var ms = new MemoryStream();
                await foto.CopyToAsync(ms);
                ms.Position = 0;
                var (pubId, url) = await _cloudinary.UploadAsync(ms, fileName, foto.ContentType);
                rutaFoto = url;
                fileId = pubId;
            }
            else
            {
                Directory.CreateDirectory(uploadsDir);
                var fileName = Guid.NewGuid().ToString("N") + ext;
                var filePath = Path.Combine(uploadsDir, fileName);
                await using var stream = new FileStream(filePath, FileMode.Create);
                await foto.CopyToAsync(stream);
                rutaFoto = $"/uploads/fotos/{fileName}";
            }

            _context.FotosRonda.Add(new FotoRonda
            {
                AreaRondaId = areaRonda.Id,
                RutaFoto = rutaFoto,
                DriveFileId = fileId,
                FechaCaptura = DateTime.Now
            });
        }

        // Crear incidencia
        _context.Incidencias.Add(new Incidencia
        {
            RondaId = ronda.Id,
            AreaRondaId = areaRonda.Id,
            Descripcion = descripcion.Trim(),
            Severidad = severidad,
            Estado = EstadoIncidencia.Abierta,
            FechaCreacion = DateTime.Now
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = "Incidencia reportada correctamente.";
        return RedirectToAction("Index");
    }

    // GET: /Incidencia/ExportarPdf/5
    [Authorize(Roles = "Guardia,Conserje,Admin")]
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
