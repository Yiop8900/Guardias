using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Guardias.Models;

public class Edificio
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100)]
    [Display(Name = "Nombre del edificio")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    [Display(Name = "Empresa")]
    public int? EmpresaId { get; set; }

    [JsonIgnore]
    public EmpresaAdministradora? Empresa { get; set; }

    [JsonIgnore]
    public ICollection<Guardia> Guardias { get; set; } = new List<Guardia>();

    [JsonIgnore]
    public ICollection<Ronda> Rondas { get; set; } = new List<Ronda>();

    [JsonIgnore]
    public ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();

    [JsonIgnore]
    public ICollection<Area> Areas { get; set; } = new List<Area>();

    [JsonIgnore]
    public ICollection<UsuarioEdificio> UsuariosEdificio { get; set; } = new List<UsuarioEdificio>();
}
