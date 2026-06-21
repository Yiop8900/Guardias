using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Guardias.Models;

public class UsuarioEdificio
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
    [StringLength(50)]
    [Display(Name = "Usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? PasswordPlain { get; set; }

    [Display(Name = "Rol")]
    public RolUsuario? Rol { get; set; }

    [Display(Name = "Administrador")]
    public bool EsAdmin { get; set; } = false;

    // Admin principal creado junto con la empresa, no cuenta contra el límite
    public bool EsPropietario { get; set; } = false;

    [Display(Name = "Empresa")]
    public int? EmpresaId { get; set; }

    [Display(Name = "Edificio")]
    public int? EdificioId { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    [JsonIgnore]
    public EmpresaAdministradora? Empresa { get; set; }

    [JsonIgnore]
    public Edificio? Edificio { get; set; }

    public RolUsuario RolEfectivo =>
        Rol ?? (EsAdmin ? RolUsuario.Admin : RolUsuario.Guardia);
}
