using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Guardias.Models;

public class Area
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100)]
    [Display(Name = "Nombre del área")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Required]
    public int EdificioId { get; set; }

    [Display(Name = "Orden")]
    public int Orden { get; set; } = 0;

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    [JsonIgnore]
    public Edificio? Edificio { get; set; }

    [JsonIgnore]
    public ICollection<AreaRonda> AreaRondas { get; set; } = new List<AreaRonda>();
}
