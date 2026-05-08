using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Guardias.Models;

public class Guardia
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100)]
    [Display(Name = "Nombre completo")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Cargo")]
    public string? Cargo { get; set; }

    [Display(Name = "Turno")]
    public Turno Turno { get; set; } = Turno.Manana;

    [Required(ErrorMessage = "Debe asignar un edificio")]
    [Display(Name = "Edificio")]
    public int EdificioId { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    public Edificio? Edificio { get; set; }

    [JsonIgnore]
    public ICollection<Ronda> Rondas { get; set; } = new List<Ronda>();

    [JsonIgnore]
    public ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
}
