using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Guardias.Data;
using Guardias.Models;

namespace Guardias.Controllers;

[Authorize(Roles = "Admin")]
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
            _context.Guardias.Remove(guardia);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Guardia eliminado correctamente.";
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
            _context.Edificios.Remove(edificio);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Edificio eliminado correctamente.";
        }
        return RedirectToAction("Edificios");
    }

    // ===== ÁREAS =====
    public async Task<IActionResult> Areas(int? edificioId)
    {
        var query = _context.Areas.Include(a => a.Edificio).AsQueryable();
        if (edificioId.HasValue)
            query = query.Where(a => a.EdificioId == edificioId.Value);

        var areas = await query.OrderBy(a => a.Edificio!.Nombre).ThenBy(a => a.Orden).ToListAsync();
        await LoadEdificiosViewBag();
        ViewBag.SelectedEdificio = edificioId;
        return View(areas);
    }

    public async Task<IActionResult> CrearArea(int? edificioId)
    {
        await LoadEdificiosViewBag();
        var area = new Area();
        if (edificioId.HasValue) area.EdificioId = edificioId.Value;
        return View(area);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearArea(Area area)
    {
        if (ModelState.IsValid)
        {
            _context.Areas.Add(area);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Área creada correctamente.";
            return RedirectToAction("Areas", new { edificioId = area.EdificioId });
        }
        await LoadEdificiosViewBag();
        return View(area);
    }

    public async Task<IActionResult> EditarArea(int id)
    {
        var area = await _context.Areas.FindAsync(id);
        if (area == null) return NotFound();
        await LoadEdificiosViewBag();
        return View(area);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarArea(int id, Area area)
    {
        if (id != area.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            _context.Update(area);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Área actualizada.";
            return RedirectToAction("Areas", new { edificioId = area.EdificioId });
        }
        await LoadEdificiosViewBag();
        return View(area);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarArea(int id)
    {
        var area = await _context.Areas.FindAsync(id);
        if (area != null)
        {
            int edificioId = area.EdificioId;
            _context.Areas.Remove(area);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Área eliminada correctamente.";
            return RedirectToAction("Areas", new { edificioId });
        }
        return RedirectToAction("Areas");
    }

    // ===== USUARIOS =====
    public async Task<IActionResult> Usuarios()
    {
        var usuarios = await _context.UsuariosEdificio
            .Include(u => u.Edificio)
            .OrderBy(u => u.Edificio!.Nombre)
            .ThenBy(u => u.NombreUsuario)
            .ToListAsync();
        return View(usuarios);
    }

    public async Task<IActionResult> CrearUsuario()
    {
        await LoadEdificiosViewBag();
        return View(new UsuarioEdificio());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearUsuario(UsuarioEdificio usuario, string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
            ModelState.AddModelError("password", "La contraseña debe tener al menos 4 caracteres.");

        // Check duplicate username
        if (await _context.UsuariosEdificio.AnyAsync(u => u.NombreUsuario == usuario.NombreUsuario))
            ModelState.AddModelError("NombreUsuario", "Ya existe un usuario con ese nombre.");

        if (ModelState.IsValid)
        {
            var hasher = new PasswordHasher<UsuarioEdificio>();
            usuario.PasswordHash = hasher.HashPassword(usuario, password);
            usuario.PasswordPlain = password;
            _context.UsuariosEdificio.Add(usuario);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Usuario creado correctamente.";
            return RedirectToAction("Usuarios");
        }
        await LoadEdificiosViewBag();
        return View(usuario);
    }

    public async Task<IActionResult> EditarUsuario(int id)
    {
        var usuario = await _context.UsuariosEdificio.FindAsync(id);
        if (usuario == null) return NotFound();
        await LoadEdificiosViewBag();
        return View(usuario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarUsuario(int id, UsuarioEdificio usuario, string? nuevaPassword)
    {
        // Check duplicate username for other users
        if (await _context.UsuariosEdificio.AnyAsync(u => u.NombreUsuario == usuario.NombreUsuario && u.Id != id))
            ModelState.AddModelError("NombreUsuario", "Ya existe un usuario con ese nombre.");

        if (ModelState.IsValid)
        {
            var existing = await _context.UsuariosEdificio.FindAsync(id);
            if (existing == null) return NotFound();

            existing.NombreUsuario = usuario.NombreUsuario;
            existing.EdificioId = usuario.EdificioId;
            existing.Activo = usuario.Activo;

            if (!string.IsNullOrWhiteSpace(nuevaPassword) && nuevaPassword.Length >= 4)
            {
                var hasher = new PasswordHasher<UsuarioEdificio>();
                existing.PasswordHash = hasher.HashPassword(existing, nuevaPassword);
                existing.PasswordPlain = nuevaPassword;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Usuario actualizado.";
            return RedirectToAction("Usuarios");
        }
        await LoadEdificiosViewBag();
        return View(usuario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarUsuario(int id)
    {
        var usuario = await _context.UsuariosEdificio.FindAsync(id);
        if (usuario != null)
        {
            _context.UsuariosEdificio.Remove(usuario);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Usuario eliminado correctamente.";
        }
        return RedirectToAction("Usuarios");
    }

    // ===== TAREAS =====
    public async Task<IActionResult> Tareas(int? edificioId)
    {
        var query = _context.Tareas
            .Include(t => t.Guardia)
            .Include(t => t.Edificio)
            .AsQueryable();

        if (edificioId.HasValue)
            query = query.Where(t => t.EdificioId == edificioId.Value);

        var tareas = await query
            .OrderBy(t => t.HoraProgramada)
            .ThenBy(t => t.Titulo)
            .ToListAsync();

        await LoadEdificiosViewBag();
        ViewBag.SelectedEdificio = edificioId;
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
    public async Task<IActionResult> Rondas(int? edificioId)
    {
        var query = _context.Rondas
            .Include(r => r.Guardia)
            .Include(r => r.Edificio)
            .Include(r => r.AreaRondas).ThenInclude(ar => ar.Fotos)
            .Include(r => r.Incidencias)
            .AsQueryable();

        if (edificioId.HasValue)
            query = query.Where(r => r.EdificioId == edificioId.Value);

        var rondas = await query
            .OrderByDescending(r => r.FechaHora)
            .ToListAsync();

        await LoadEdificiosViewBag();
        ViewBag.SelectedEdificio = edificioId;
        return View(rondas);
    }

    public async Task<IActionResult> DetalleRonda(int id)
    {
        var ronda = await _context.Rondas
            .Include(r => r.Guardia)
            .Include(r => r.Edificio)
            .Include(r => r.AreaRondas).ThenInclude(ar => ar.Area)
            .Include(r => r.AreaRondas).ThenInclude(ar => ar.Fotos)
            .Include(r => r.Incidencias)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (ronda == null) return NotFound();
        return View(ronda);
    }

    public async Task<IActionResult> ExportarPdfRonda(int id)
    {
        var ronda = await _context.Rondas
            .Include(r => r.Guardia)
            .Include(r => r.Edificio)
            .Include(r => r.AreaRondas).ThenInclude(ar => ar.Area)
            .Include(r => r.AreaRondas).ThenInclude(ar => ar.Fotos)
            .Include(r => r.Incidencias)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (ronda == null) return NotFound();
        return View(ronda);
    }

    public async Task<IActionResult> FirmarRonda(int id)
    {
        var ronda = await _context.Rondas
            .Include(r => r.Guardia)
            .Include(r => r.Edificio)
            .Include(r => r.AreaRondas).ThenInclude(ar => ar.Area)
            .Include(r => r.AreaRondas).ThenInclude(ar => ar.Fotos)
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
                .Include(r => r.AreaRondas).ThenInclude(ar => ar.Area)
                .Include(r => r.AreaRondas).ThenInclude(ar => ar.Fotos)
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
    public async Task<IActionResult> Incidencias(int? edificioId)
    {
        var query = _context.Incidencias
            .Include(i => i.Ronda)
                .ThenInclude(r => r!.Guardia)
            .Include(i => i.Ronda)
                .ThenInclude(r => r!.Edificio)
            .AsQueryable();

        if (edificioId.HasValue)
            query = query.Where(i => i.Ronda!.EdificioId == edificioId.Value);

        var incidencias = await query
            .OrderByDescending(i => i.FechaCreacion)
            .ToListAsync();

        await LoadEdificiosViewBag();
        ViewBag.SelectedEdificio = edificioId;
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

    public async Task<IActionResult> ExportarPdfIncidencia(int id)
    {
        var incidencia = await _context.Incidencias
            .Include(i => i.Ronda).ThenInclude(r => r!.Guardia)
            .Include(i => i.Ronda).ThenInclude(r => r!.Edificio)
            .Include(i => i.Ronda).ThenInclude(r => r!.AreaRondas).ThenInclude(ar => ar.Area)
            .Include(i => i.Ronda).ThenInclude(r => r!.AreaRondas).ThenInclude(ar => ar.Fotos)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (incidencia == null) return NotFound();
        return View("~/Views/Incidencia/ExportarPdf.cshtml", incidencia);
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
        ViewBag.EdificiosList = edificios; // List<Edificio> para vistas que iteran manualmente
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
