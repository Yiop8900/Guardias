using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Guardias.Models;

public class Ronda
{
    public int Id { get; set; }

    [Required]
    public int GuardiaId { get; set; }

    [Required]
    public int EdificioId { get; set; }

    public DateTime FechaHora { get; set; } = DateTime.Now;

    public EstadoRonda Estado { get; set; } = EstadoRonda.EnCurso;

    [StringLength(1000)]
    [Display(Name = "Reporte de incidencias")]
    public string? ReporteIncidencias { get; set; }

    [StringLength(100)]
    [Display(Name = "Firmado por")]
    public string? FirmadoPor { get; set; }

    public DateTime? FechaFirma { get; set; }

    public Guardia? Guardia { get; set; }
    public Edificio? Edificio { get; set; }

    [JsonIgnore]
    public ICollection<FotoRonda> Fotos { get; set; } = new List<FotoRonda>();

    [JsonIgnore]
    public ICollection<Incidencia> Incidencias { get; set; } = new List<Incidencia>();
}
