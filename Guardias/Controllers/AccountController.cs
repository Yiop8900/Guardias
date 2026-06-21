using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Guardias.Data;
using Guardias.Models;

namespace Guardias.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AccountController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectByRole();
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string usuario, string password, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Ingresa usuario y contraseña.");
            return View();
        }

        // Credenciales del SuperAdmin global en appsettings
        var adminUser = _config["Admin:Usuario"] ?? "admin";
        var adminPass = _config["Admin:Password"] ?? "admin123";

        if (usuario == adminUser && password == adminPass)
        {
            await SignInWithClaims(usuario, RolUsuario.SuperAdmin, empresaId: null, edificioId: null, edificioNombre: null);
            return RedirectToAction("Index", "SuperAdmin");
        }

        // Usuarios registrados en BD
        var usuarioEdificio = await _context.UsuariosEdificio
            .Include(u => u.Edificio)
            .Include(u => u.Empresa)
            .Where(u => u.NombreUsuario == usuario && u.Activo)
            .FirstOrDefaultAsync();

        if (usuarioEdificio != null)
        {
            var hasher = new PasswordHasher<UsuarioEdificio>();
            var result = hasher.VerifyHashedPassword(usuarioEdificio, usuarioEdificio.PasswordHash, password);
            if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                var rol = usuarioEdificio.RolEfectivo;
                await SignInWithClaims(
                    usuario,
                    rol,
                    empresaId: usuarioEdificio.EmpresaId,
                    edificioId: usuarioEdificio.EdificioId,
                    edificioNombre: usuarioEdificio.Edificio?.Nombre);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return rol switch
                {
                    RolUsuario.SuperAdmin => RedirectToAction("Index", "SuperAdmin"),
                    RolUsuario.Admin or RolUsuario.JefeOperaciones or RolUsuario.Mayordomo => RedirectToAction("Index", "Admin"),
                    _ => RedirectToAction("Historial", "Ronda")
                };
            }
        }

        ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    private async Task SignInWithClaims(string nombreUsuario, RolUsuario rol, int? empresaId, int? edificioId, string? edificioNombre)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, nombreUsuario),
            new Claim(ClaimTypes.Role, rol.ToString()),
            new Claim("Rol", rol.ToString()),
            new Claim("EmpresaId", empresaId?.ToString() ?? "0"),
            new Claim("EdificioId", edificioId?.ToString() ?? "0"),
            new Claim("EdificioNombre", edificioNombre ?? ""),
        };

        // Roles que acceden al panel Admin
        if (rol.TieneAccesoAdmin() || rol == RolUsuario.Mayordomo)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    private IActionResult RedirectByRole()
    {
        if (User.IsInRole("SuperAdmin")) return RedirectToAction("Index", "SuperAdmin");
        if (User.IsInRole("Admin")) return RedirectToAction("Index", "Admin");
        return RedirectToAction("Historial", "Ronda");
    }
}
