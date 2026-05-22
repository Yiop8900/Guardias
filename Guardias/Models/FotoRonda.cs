using System.Text.Json.Serialization;

namespace Guardias.Models;

public class FotoRonda
{
    public int Id { get; set; }
    public int AreaRondaId { get; set; }
    public string RutaFoto { get; set; } = string.Empty;
    public string? DriveFileId { get; set; }
    public DateTime FechaCaptura { get; set; } = DateTime.Now;

    [JsonIgnore]
    public AreaRonda? AreaRonda { get; set; }
}
