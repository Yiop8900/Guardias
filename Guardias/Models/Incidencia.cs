using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Guardias.Models;

public class Incidencia
{
    public int Id { get; set; }

    [Required]
    public int RondaId { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria")]
    [StringLength(1000)]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    [Display(Name = "Severidad")]
    public SeveridadIncidencia Severidad { get; set; } = SeveridadIncidencia.Leve;

    [Display(Name = "Estado")]
    public EstadoIncidencia Estado { get; set; } = EstadoIncidencia.Abierta;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? FechaCierre { get; set; }

    [StringLength(500)]
    [Display(Name = "Notas de cierre")]
    public string? NotasCierre { get; set; }

    [JsonIgnore]
    public Ronda? Ronda { get; set; }
}
