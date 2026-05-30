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

    [Display(Name = "Administrador")]
    public bool EsAdmin { get; set; } = false;

    [Display(Name = "Edificio")]
    public int? EdificioId { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    [JsonIgnore]
    public Edificio? Edificio { get; set; }
}
