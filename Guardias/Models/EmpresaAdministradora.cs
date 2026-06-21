using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Guardias.Models;

public class EmpresaAdministradora
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(150)]
    [Display(Name = "Nombre de la empresa")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Display(Name = "Activa")]
    public bool Activa { get; set; } = true;

    [Required]
    [Range(1, 100, ErrorMessage = "Debe ser entre 1 y 100")]
    [Display(Name = "Límite de administradores")]
    public int LimiteAdmins { get; set; } = 5;

    [Required]
    [Range(1, 500, ErrorMessage = "Debe ser entre 1 y 500")]
    [Display(Name = "Límite de edificios")]
    public int LimiteEdificios { get; set; } = 10;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    [JsonIgnore]
    public ICollection<Edificio> Edificios { get; set; } = new List<Edificio>();

    [JsonIgnore]
    public ICollection<UsuarioEdificio> Usuarios { get; set; } = new List<UsuarioEdificio>();
}
