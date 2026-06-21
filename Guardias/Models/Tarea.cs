using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Guardias.Models;

public class Tarea
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El título es obligatorio")]
    [StringLength(200)]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Display(Name = "Hora programada")]
    public TimeOnly? HoraProgramada { get; set; }

    [Display(Name = "Turno")]
    public Turno Turno { get; set; } = Turno.Todos;

    [Display(Name = "Guardia asignado")]
    public int? GuardiaId { get; set; }

    [Display(Name = "Edificio")]
    public int? EdificioId { get; set; }

    [Display(Name = "Estado")]
    public EstadoTarea Estado { get; set; } = EstadoTarea.Pendiente;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public DateTime? FechaCompletada { get; set; }

    // Calculado: true si fue completada HOY
    public bool CompletadaHoy => FechaCompletada?.Date == DateTime.Today;

    public Guardia? Guardia { get; set; }

    [JsonIgnore]
    public Edificio? Edificio { get; set; }

    public ICollection<TareaArchivo> Archivos { get; set; } = new List<TareaArchivo>();
}
