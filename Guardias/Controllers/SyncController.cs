using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Guardias.Data;
using Guardias.Models;

namespace Guardias.Controllers;

[Authorize]
[IgnoreAntiforgeryToken]
[Route("Sync")]
public class SyncController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public SyncController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    private int GetEdificioId() =>
        int.TryParse(User.FindFirstValue("EdificioId"), out var id) ? id : 0;

    // POST /Sync/CompletarTarea
    [HttpPost("CompletarTarea")]
    public async Task<IActionResult> CompletarTarea([FromBody] SyncCompletarRequest req)
    {
        if (req == null || req.TareaId <= 0)
            return BadRequest();

        var edificioId = GetEdificioId();
        var tarea = await _context.Tareas
            .Include(t => t.Archivos)
            .FirstOrDefaultAsync(t => t.Id == req.TareaId && t.EdificioId == edificioId);

        if (tarea == null) return NotFound();

        // Solo completar si tiene archivos (puede que se hayan subido antes offline)
        // Permitimos completar offline aunque los archivos se sincronicen después
        var fecha = req.Fecha ?? DateTime.Now;

        // No reemplazar si ya fue completada hoy por una sync anterior
        if (tarea.FechaCompletada?.Date != DateTime.Today)
            tarea.FechaCompletada = fecha;

        await _context.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    // POST /Sync/SubirArchivo/{tareaId}
    [HttpPost("SubirArchivo/{tareaId}")]
    public async Task<IActionResult> SubirArchivo(int tareaId, List<IFormFile> archivos)
    {
        var edificioId = GetEdificioId();
        var tarea = await _context.Tareas
            .FirstOrDefaultAsync(t => t.Id == tareaId && t.EdificioId == edificioId);

        if (tarea == null) return NotFound();
        if (archivos == null || !archivos.Any()) return BadRequest();

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
            if (!extensionesPermitidas.Contains(ext)) continue;

            var nombreGuardado = $"{Guid.NewGuid()}{ext}";
            var rutaCompleta = Path.Combine(uploadPath, nombreGuardado);

            using var stream = new FileStream(rutaCompleta, FileMode.Create);
            await file.CopyToAsync(stream);

            var esImagen = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".heic" }
                .Contains(ext, StringComparer.OrdinalIgnoreCase);

            _context.TareaArchivos.Add(new TareaArchivo
            {
                TareaId = tareaId,
                NombreOriginal = file.FileName,
                NombreArchivo = nombreGuardado,
                TipoMime = file.ContentType,
                EsImagen = esImagen,
                SubidoPor = User.Identity!.Name ?? ""
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    // POST /Sync/CheckArea — sincroniza un área completada offline
    [HttpPost("CheckArea")]
    public async Task<IActionResult> CheckArea([FromForm] SyncCheckAreaRequest req, List<IFormFile>? fotos)
    {
        var edificioId = GetEdificioId();
        var ronda = await _context.Rondas
            .Include(r => r.AreaRondas)
            .FirstOrDefaultAsync(r => r.Id == req.RondaId && r.EdificioId == edificioId);

        if (ronda == null) return NotFound();

        var areaRonda = ronda.AreaRondas.FirstOrDefault(ar => ar.Id == req.AreaRondaId);
        if (areaRonda == null) return NotFound();

        if (areaRonda.Completada) return Ok(new { ok = true, already = true });

        areaRonda.Completada = true;
        areaRonda.Notas = req.Notas;
        areaRonda.FechaCompletada = DateTime.Now;

        // Guardar fotos
        if (fotos != null && fotos.Any())
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "fotos");
            Directory.CreateDirectory(uploadsDir);
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            foreach (var foto in fotos.Where(f => f.Length > 0))
            {
                var ext = Path.GetExtension(foto.FileName).ToLowerInvariant();
                if (!allowed.Contains(ext)) continue;
                var fileName = Guid.NewGuid().ToString("N") + ext;
                var path = Path.Combine(uploadsDir, fileName);
                await using var stream = new FileStream(path, FileMode.Create);
                await foto.CopyToAsync(stream);
                _context.FotosRonda.Add(new FotoRonda
                {
                    AreaRondaId = req.AreaRondaId,
                    RutaFoto = $"/uploads/fotos/{fileName}",
                    FechaCaptura = DateTime.Now
                });
            }
        }

        // Incidencia opcional
        if (!string.IsNullOrWhiteSpace(req.IncDescripcion))
        {
            _context.Incidencias.Add(new Incidencia
            {
                RondaId = req.RondaId,
                AreaRondaId = req.AreaRondaId,
                Descripcion = req.IncDescripcion,
                Severidad = req.IncSeveridad,
                Estado = EstadoIncidencia.Abierta,
                FechaCreacion = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    // GET /Sync/Token — devuelve el antiforgery token para usar en JS
    [HttpGet("Token")]
    public IActionResult Token()
    {
        // Generamos un token simple basado en el usuario para identificar syncs
        var usuario = User.Identity?.Name ?? "";
        var token = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{usuario}:{DateTime.Today:yyyyMMdd}")
        );
        return Json(new { token });
    }

    // GET /offline — página de fallback offline
    [AllowAnonymous]
    [HttpGet("/offline")]
    public IActionResult Offline() => View();
}

public class SyncCompletarRequest
{
    public int TareaId { get; set; }
    public DateTime? Fecha { get; set; }
}

public class SyncCheckAreaRequest
{
    public int RondaId { get; set; }
    public int AreaRondaId { get; set; }
    public string? Notas { get; set; }
    public string? IncDescripcion { get; set; }
    public SeveridadIncidencia IncSeveridad { get; set; } = SeveridadIncidencia.Leve;
}
