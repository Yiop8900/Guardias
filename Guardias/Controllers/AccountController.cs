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
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Index", "Admin");
            return RedirectToAction("Historial", "Ronda");
        }
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

        // Check admin credentials
        var adminUser = _config["Admin:Usuario"] ?? "admin";
        var adminPass = _config["Admin:Password"] ?? "admin123";

        if (usuario == adminUser && password == adminPass)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("EdificioId", "0"),
                new Claim("EdificioNombre", "Administrador")
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return RedirectToAction("Index", "Admin");
        }

        // Check building users
        var usuarioEdificio = await _context.UsuariosEdificio
            .Include(u => u.Edificio)
            .Where(u => u.NombreUsuario == usuario && u.Activo)
            .FirstOrDefaultAsync();

        if (usuarioEdificio != null)
        {
            var hasher = new PasswordHasher<UsuarioEdificio>();
            var result = hasher.VerifyHashedPassword(usuarioEdificio, usuarioEdificio.PasswordHash, password);
            if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, usuario),
                    new Claim(ClaimTypes.Role, "Guardia"),
                    new Claim("EdificioId", usuarioEdificio.EdificioId.ToString()),
                    new Claim("EdificioNombre", usuarioEdificio.Edificio?.Nombre ?? "")
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Historial", "Ronda");
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
}
