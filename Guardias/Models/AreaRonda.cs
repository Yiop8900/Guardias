using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Guardias.Models;

public class AreaRonda
{
    public int Id { get; set; }

    [Required]
    public int RondaId { get; set; }

    [Required]
    public int AreaId { get; set; }

    [Display(Name = "Completada")]
    public bool Completada { get; set; } = false;

    [StringLength(500)]
    [Display(Name = "Notas")]
    public string? Notas { get; set; }

    public DateTime? FechaCompletada { get; set; }

    [JsonIgnore]
    public Ronda? Ronda { get; set; }

    [JsonIgnore]
    public Area? Area { get; set; }

    [JsonIgnore]
    public ICollection<FotoRonda> Fotos { get; set; } = new List<FotoRonda>();

    [JsonIgnore]
    public ICollection<Incidencia> Incidencias { get; set; } = new List<Incidencia>();
}
