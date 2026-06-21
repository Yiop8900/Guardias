using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Guardias.Data;
using Guardias.Models;

namespace Guardias.Controllers;

[Authorize(Roles = "Admin,Mayordomo")]
public class AdminController : Controller
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    // Retorna el EmpresaId del usuario autenticado; null = SuperAdmin (ve todo)
    private int? GetEmpresaId()
    {
        var val = User.FindFirstValue("EmpresaId");
        if (val == null || val == "0") return null;
        return int.TryParse(val, out var id) ? id : null;
    }

    private bool EsSuperAdmin() => User.IsInRole("SuperAdmin");

    // Retorna el EdificioId fijo del usuario (Mayordomo/Conserje); null = sin restricción de edificio
    private int? GetEdificioIdFijo()
    {
        var val = User.FindFirstValue("EdificioId");
        if (val == null || val == "0") return null;
        return int.TryParse(val, out var id) ? id : null;
    }

    // ===== DASHBOARD =====
    public async Task<IActionResult> Index()
    {
        var empresaId = GetEmpresaId();

        if (empresaId.HasValue)
        {
            ViewBag.TotalGuardias = await _context.Guardias.CountAsync(g => g.Activo && g.Edificio!.EmpresaId == empresaId.Value);
            ViewBag.TotalEdificios = await _context.Edificios.CountAsync(e => e.Activo && e.EmpresaId == empresaId.Value);
            ViewBag.TotalRondas = await _context.Rondas.CountAsync(r => r.Edificio!.EmpresaId == empresaId.Value);
            ViewBag.RondasHoy = await _context.Rondas.CountAsync(r => r.FechaHora.Date == DateTime.Today && r.Edificio!.EmpresaId == empresaId.Value);
            ViewBag.IncidenciasAbiertas = await _context.Incidencias.CountAsync(i => i.Estado == EstadoIncidencia.Abierta && i.Ronda!.Edificio!.EmpresaId == empresaId.Value);
            ViewBag.TareasPendientes = await _context.Tareas.CountAsync(t => t.Estado == EstadoTarea.Pendiente && t.Edificio!.EmpresaId == empresaId.Value);
            ViewBag.RondasPendientesFirma = await _context.Rondas.CountAsync(r => r.Estado == EstadoRonda.Finalizada && r.Edificio!.EmpresaId == empresaId.Value);
        }
        else
        {
            ViewBag.TotalGuardias = await _context.Guardias.CountAsync(g => g.Activo);
            ViewBag.TotalEdificios = await _context.Edificios.CountAsync(e => e.Activo);
            ViewBag.TotalRondas = await _context.Rondas.CountAsync();
            ViewBag.RondasHoy = await _context.Rondas.CountAsync(r => r.FechaHora.Date == DateTime.Today);
            ViewBag.IncidenciasAbiertas = await _context.Incidencias.CountAsync(i => i.Estado == EstadoIncidencia.Abierta);
            ViewBag.TareasPendientes = await _context.Tareas.CountAsync(t => t.Estado == EstadoTarea.Pendiente);
            ViewBag.RondasPendientesFirma = await _context.Rondas.CountAsync(r => r.Estado == EstadoRonda.Finalizada);
        }

        ViewBag.EmpresaId = empresaId;
        return View();
    }

    // ===== GUARDIAS =====
    public async Task<IActionResult> Guardias()
    {
        var empresaId = GetEmpresaId();
        var query = _context.Guardias.Include(g => g.Edificio).AsQueryable();
        if (empresaId.HasValue)
            query = query.Where(g => g.Edificio!.EmpresaId == empresaId.Value);
        var guardias = await query.OrderBy(g => g.Nombre).ToListAsync();
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
        var empresaId = GetEmpresaId();
        var query = _context.Edificios.Include(e => e.Empresa).AsQueryable();
        if (empresaId.HasValue)
            query = query.Where(e => e.EmpresaId == empresaId.Value);
        var edificios = await query.OrderBy(e => e.Nombre).ToListAsync();
        return View(edificios);
    }

    public async Task<IActionResult> CrearEdificio()
    {
        if (EsSuperAdmin()) await LoadEmpresasViewBag();
        return View(new Edificio());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearEdificio(Edificio edificio)
    {
        // Si no es SuperAdmin, asignar empresa automáticamente
        if (!EsSuperAdmin())
        {
            var empresaId = GetEmpresaId();
            edificio.EmpresaId = empresaId;
        }

        // Validar límite de edificios de la empresa
        if (edificio.EmpresaId.HasValue)
        {
            var empresa = await _context.EmpresasAdministradoras.FindAsync(edificio.EmpresaId.Value);
            if (empresa != null)
            {
                var totalEdificios = await _context.Edificios.CountAsync(e => e.EmpresaId == edificio.EmpresaId.Value);
                if (totalEdificios >= empresa.LimiteEdificios)
                {
                    ModelState.AddModelError("", $"La empresa ha alcanzado el límite de {empresa.LimiteEdificios} edificios.");
                }
            }
        }

        if (ModelState.IsValid)
        {
            _context.Edificios.Add(edificio);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Edificio creado correctamente.";
            return RedirectToAction("Edificios");
        }
        if (EsSuperAdmin()) await LoadEmpresasViewBag();
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
            var areaIds = await _context.Areas
                .Where(a => a.EdificioId == id)
                .Select(a => a.Id)
                .ToListAsync();

            if (areaIds.Any())
            {
                var areaRondaIds = await _context.AreaRondas
                    .Where(ar => areaIds.Contains(ar.AreaId))
                    .Select(ar => ar.Id)
                    .ToListAsync();

                if (areaRondaIds.Any())
                {
                    var fotos = await _context.FotosRonda
                        .Where(f => areaRondaIds.Contains(f.AreaRondaId))
                        .ToListAsync();
                    _context.FotosRonda.RemoveRange(fotos);
                }

                var areaRondas = await _context.AreaRondas
                    .Where(ar => areaIds.Contains(ar.AreaId))
                    .ToListAsync();
                _context.AreaRondas.RemoveRange(areaRondas);

                var areas = await _context.Areas
                    .Where(a => areaIds.Contains(a.Id))
                    .ToListAsync();
                _context.Areas.RemoveRange(areas);
            }

            var rondas = await _context.Rondas
                .Where(r => r.EdificioId == id)
                .ToListAsync();
            _context.Rondas.RemoveRange(rondas);

            _context.Edificios.Remove(edificio);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Edificio eliminado correctamente.";
        }
        return RedirectToAction("Edificios");
    }

    // ===== ÁREAS =====
    public async Task<IActionResult> Areas(int? edificioId)
    {
        var empresaId = GetEmpresaId();
        var query = _context.Areas.Include(a => a.Edificio).AsQueryable();
        if (empresaId.HasValue)
            query = query.Where(a => a.Edificio!.EmpresaId == empresaId.Value);
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
        var empresaId = GetEmpresaId();
        var query = _context.UsuariosEdificio
            .Include(u => u.Edificio)
            .Include(u => u.Empresa)
            .AsQueryable();
        if (empresaId.HasValue)
            query = query.Where(u => u.EmpresaId == empresaId.Value);
        else
            query = query.Where(u => u.Rol != RolUsuario.SuperAdmin); // SuperAdmin no muestra sus propios cuentas aquí
        var usuarios = await query
            .OrderBy(u => u.Empresa!.Nombre)
            .ThenBy(u => u.NombreUsuario)
            .ToListAsync();
        return View(usuarios);
    }

    public async Task<IActionResult> CrearUsuario()
    {
        await LoadEdificiosViewBag();
        if (EsSuperAdmin()) await LoadEmpresasViewBag();
        return View(new UsuarioEdificio());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearUsuario(UsuarioEdificio usuario, string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
            ModelState.AddModelError("password", "La contraseña debe tener al menos 4 caracteres.");

        if (await _context.UsuariosEdificio.AnyAsync(u => u.NombreUsuario == usuario.NombreUsuario))
            ModelState.AddModelError("NombreUsuario", "Ya existe un usuario con ese nombre.");

        // Asignar empresa leyendo desde la BD (más confiable que el claim)
        if (!EsSuperAdmin())
        {
            var nombreActual = User.Identity!.Name;
            var usuarioActual = await _context.UsuariosEdificio
                .FirstOrDefaultAsync(u => u.NombreUsuario == nombreActual);
            usuario.EmpresaId = usuarioActual?.EmpresaId ?? GetEmpresaId();

            if (!usuario.EmpresaId.HasValue)
            {
                ModelState.AddModelError("", "Tu cuenta no tiene una empresa asignada. Contacta al Super Admin.");
                await LoadEdificiosViewBag();
                if (EsSuperAdmin()) await LoadEmpresasViewBag();
                return View(usuario);
            }
        }

        var rolEfectivo = usuario.Rol ?? RolUsuario.Guardia;

        // JefeOperaciones solo puede crear Mayordomo, Conserje y Guardia
        var rolCreador = User.FindFirstValue("Rol");
        if (rolCreador == "JefeOperaciones" && rolEfectivo == RolUsuario.Admin)
        {
            ModelState.AddModelError("", "El Jefe de Operaciones no puede crear administradores.");
            await LoadEdificiosViewBag();
            return View(usuario);
        }

        usuario.EsAdmin = rolEfectivo.TieneAccesoAdmin();

        // Validar límite de admins (el propietario no cuenta)
        if (rolEfectivo == RolUsuario.Admin && usuario.EmpresaId.HasValue)
        {
            var empresa = await _context.EmpresasAdministradoras.FindAsync(usuario.EmpresaId.Value);
            if (empresa != null)
            {
                var totalAdmins = await _context.UsuariosEdificio.CountAsync(u =>
                    u.EmpresaId == usuario.EmpresaId.Value &&
                    !u.EsPropietario &&
                    (u.Rol == RolUsuario.Admin || u.EsAdmin));
                if (totalAdmins >= empresa.LimiteAdmins)
                    ModelState.AddModelError("", $"La empresa ha alcanzado el límite de {empresa.LimiteAdmins} administradores.");
            }
        }

        // Roles sin edificio: Admin, JefeOperaciones
        if (rolEfectivo is RolUsuario.Admin or RolUsuario.JefeOperaciones)
            usuario.EdificioId = null;
        else if (usuario.EdificioId == null)
            ModelState.AddModelError("EdificioId", "Selecciona un edificio para el usuario.");

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
        if (EsSuperAdmin()) await LoadEmpresasViewBag();
        return View(usuario);
    }

    public async Task<IActionResult> EditarUsuario(int id)
    {
        var usuario = await _context.UsuariosEdificio.FindAsync(id);
        if (usuario == null) return NotFound();
        await LoadEdificiosViewBag();
        if (EsSuperAdmin()) await LoadEmpresasViewBag();
        return View(usuario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarUsuario(int id, UsuarioEdificio usuario, string? nuevaPassword)
    {
        if (await _context.UsuariosEdificio.AnyAsync(u => u.NombreUsuario == usuario.NombreUsuario && u.Id != id))
            ModelState.AddModelError("NombreUsuario", "Ya existe un usuario con ese nombre.");

        var rolEfectivo = usuario.Rol ?? RolUsuario.Guardia;
        if (rolEfectivo is not (RolUsuario.Admin or RolUsuario.JefeOperaciones) && usuario.EdificioId == null)
            ModelState.AddModelError("EdificioId", "Selecciona un edificio para el usuario.");

        if (ModelState.IsValid)
        {
            var existing = await _context.UsuariosEdificio.FindAsync(id);
            if (existing == null) return NotFound();

            existing.NombreUsuario = usuario.NombreUsuario;
            existing.Rol = usuario.Rol;
            existing.EsAdmin = rolEfectivo.TieneAccesoAdmin();
            existing.EdificioId = rolEfectivo is RolUsuario.Admin or RolUsuario.JefeOperaciones ? null : usuario.EdificioId;
            existing.EmpresaId = EsSuperAdmin() ? usuario.EmpresaId : existing.EmpresaId;
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
        if (EsSuperAdmin()) await LoadEmpresasViewBag();
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
        var empresaId = GetEmpresaId();
        var edificioFijo = GetEdificioIdFijo();
        if (edificioFijo.HasValue) edificioId = edificioFijo; // el edificio del usuario tiene prioridad

        var query = _context.Tareas
            .Include(t => t.Guardia)
            .Include(t => t.Edificio)
            .Include(t => t.Archivos)
            .AsQueryable();

        if (empresaId.HasValue)
            query = query.Where(t => t.Edificio!.EmpresaId == empresaId.Value);
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

    public async Task<IActionResult> DetalleTarea(int id)
    {
        var empresaId = GetEmpresaId();
        var query = _context.Tareas
            .Include(t => t.Guardia)
            .Include(t => t.Edificio)
            .Include(t => t.Archivos)
            .AsQueryable();

        if (empresaId.HasValue)
            query = query.Where(t => t.Edificio!.EmpresaId == empresaId.Value);

        var tarea = await query.FirstOrDefaultAsync(t => t.Id == id);
        if (tarea == null) return NotFound();
        return View(tarea);
    }

    // ===== RONDAS =====
    public async Task<IActionResult> Rondas(int? edificioId)
    {
        var empresaId = GetEmpresaId();
        var edificioFijo = GetEdificioIdFijo();
        if (edificioFijo.HasValue) edificioId = edificioFijo;

        var query = _context.Rondas
            .Include(r => r.Guardia)
            .Include(r => r.Edificio)
            .Include(r => r.AreaRondas).ThenInclude(ar => ar.Fotos)
            .Include(r => r.Incidencias)
            .AsQueryable();

        if (empresaId.HasValue)
            query = query.Where(r => r.Edificio!.EmpresaId == empresaId.Value);
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
        var empresaId = GetEmpresaId();
        var edificioFijo = GetEdificioIdFijo();
        if (edificioFijo.HasValue) edificioId = edificioFijo;

        var query = _context.Incidencias
            .Include(i => i.Ronda)
                .ThenInclude(r => r!.Guardia)
            .Include(i => i.Ronda)
                .ThenInclude(r => r!.Edificio)
            .AsQueryable();

        if (empresaId.HasValue)
            query = query.Where(i => i.Ronda!.Edificio!.EmpresaId == empresaId.Value);
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
        var empresaId = GetEmpresaId();
        var query = _context.Edificios.Where(e => e.Activo).AsQueryable();
        if (empresaId.HasValue)
            query = query.Where(e => e.EmpresaId == empresaId.Value);
        var edificios = await query.OrderBy(e => e.Nombre).ToListAsync();
        ViewBag.Edificios = new SelectList(edificios, "Id", "Nombre");
        ViewBag.EdificiosList = edificios;
    }

    private async Task LoadGuardiasViewBag()
    {
        var empresaId = GetEmpresaId();
        var query = _context.Guardias.Where(g => g.Activo).AsQueryable();
        if (empresaId.HasValue)
            query = query.Where(g => g.Edificio!.EmpresaId == empresaId.Value);
        var guardias = await query.Include(g => g.Edificio).OrderBy(g => g.Nombre).ToListAsync();
        ViewBag.GuardiasList = new SelectList(guardias, "Id", "Nombre");
    }

    private async Task LoadEmpresasViewBag()
    {
        var empresas = await _context.EmpresasAdministradoras
            .Where(e => e.Activa)
            .OrderBy(e => e.Nombre)
            .ToListAsync();
        ViewBag.EmpresasList = empresas;
    }
}
