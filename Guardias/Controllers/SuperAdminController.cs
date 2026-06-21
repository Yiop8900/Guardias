using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Guardias.Data;
using Guardias.Models;

namespace Guardias.Controllers;

[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : Controller
{
    private readonly AppDbContext _context;

    public SuperAdminController(AppDbContext context)
    {
        _context = context;
    }

    // ===== DASHBOARD =====
    public async Task<IActionResult> Index()
    {
        ViewBag.TotalEmpresas = await _context.EmpresasAdministradoras.CountAsync(e => e.Activa);
        ViewBag.TotalEdificios = await _context.Edificios.CountAsync(e => e.Activo);
        ViewBag.TotalUsuarios = await _context.UsuariosEdificio.CountAsync(u => u.Activo);
        ViewBag.TotalGuardias = await _context.Guardias.CountAsync(g => g.Activo);
        ViewBag.RondasHoy = await _context.Rondas.CountAsync(r => r.FechaHora.Date == DateTime.Today);
        ViewBag.IncidenciasAbiertas = await _context.Incidencias.CountAsync(i => i.Estado == EstadoIncidencia.Abierta);
        return View();
    }

    // ===== EMPRESAS =====
    public async Task<IActionResult> Empresas()
    {
        var empresas = await _context.EmpresasAdministradoras
            .Include(e => e.Edificios)
            .Include(e => e.Usuarios)
            .OrderBy(e => e.Nombre)
            .ToListAsync();
        return View(empresas);
    }

    public IActionResult CrearEmpresa() => View(new EmpresaAdministradora());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearEmpresa(EmpresaAdministradora empresa, string adminUsuario, string adminPassword)
    {
        if (string.IsNullOrWhiteSpace(adminUsuario))
            ModelState.AddModelError("adminUsuario", "El usuario administrador es obligatorio.");
        if (string.IsNullOrWhiteSpace(adminPassword) || adminPassword.Length < 4)
            ModelState.AddModelError("adminPassword", "La contraseña debe tener al menos 4 caracteres.");
        if (!string.IsNullOrWhiteSpace(adminUsuario) && await _context.UsuariosEdificio.AnyAsync(u => u.NombreUsuario == adminUsuario))
            ModelState.AddModelError("adminUsuario", "Ya existe un usuario con ese nombre.");

        if (ModelState.IsValid)
        {
            empresa.FechaCreacion = DateTime.Now;
            _context.EmpresasAdministradoras.Add(empresa);
            await _context.SaveChangesAsync();

            var hasher = new PasswordHasher<UsuarioEdificio>();
            var adminUser = new UsuarioEdificio
            {
                NombreUsuario = adminUsuario,
                Rol = RolUsuario.Admin,
                EsAdmin = true,
                EsPropietario = true,
                EmpresaId = empresa.Id,
                Activo = true,
                PasswordPlain = adminPassword
            };
            adminUser.PasswordHash = hasher.HashPassword(adminUser, adminPassword);
            _context.UsuariosEdificio.Add(adminUser);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Empresa '{empresa.Nombre}' creada con el administrador '{adminUsuario}'.";
            return RedirectToAction("Empresas");
        }
        return View(empresa);
    }

    public async Task<IActionResult> EditarEmpresa(int id)
    {
        var empresa = await _context.EmpresasAdministradoras.FindAsync(id);
        if (empresa == null) return NotFound();
        return View(empresa);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarEmpresa(int id, EmpresaAdministradora empresa)
    {
        if (id != empresa.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            _context.Update(empresa);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Empresa actualizada.";
            return RedirectToAction("Empresas");
        }
        return View(empresa);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarEmpresa(int id)
    {
        var empresa = await _context.EmpresasAdministradoras.FindAsync(id);
        if (empresa != null)
        {
            _context.EmpresasAdministradoras.Remove(empresa);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Empresa eliminada.";
        }
        return RedirectToAction("Empresas");
    }

    // ===== SUPER ADMINS =====
    public async Task<IActionResult> SuperAdmins()
    {
        var usuarios = await _context.UsuariosEdificio
            .Where(u => u.Rol == RolUsuario.SuperAdmin)
            .OrderBy(u => u.NombreUsuario)
            .ToListAsync();
        return View(usuarios);
    }

    public IActionResult CrearSuperAdmin() => View(new UsuarioEdificio());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearSuperAdmin(UsuarioEdificio usuario, string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
            ModelState.AddModelError("password", "La contraseña debe tener al menos 4 caracteres.");

        if (await _context.UsuariosEdificio.AnyAsync(u => u.NombreUsuario == usuario.NombreUsuario))
            ModelState.AddModelError("NombreUsuario", "Ya existe un usuario con ese nombre.");

        if (ModelState.IsValid)
        {
            var hasher = new PasswordHasher<UsuarioEdificio>();
            usuario.Rol = RolUsuario.SuperAdmin;
            usuario.EsAdmin = true;
            usuario.EdificioId = null;
            usuario.EmpresaId = null;
            usuario.PasswordHash = hasher.HashPassword(usuario, password);
            usuario.PasswordPlain = password;
            _context.UsuariosEdificio.Add(usuario);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Super Admin creado correctamente.";
            return RedirectToAction("SuperAdmins");
        }
        return View(usuario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarSuperAdmin(int id)
    {
        var usuario = await _context.UsuariosEdificio.FindAsync(id);
        if (usuario != null)
        {
            _context.UsuariosEdificio.Remove(usuario);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Super Admin eliminado.";
        }
        return RedirectToAction("SuperAdmins");
    }
}
