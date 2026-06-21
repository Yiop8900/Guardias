namespace Guardias.Models;

public class TareaArchivo
{
    public int Id { get; set; }
    public int TareaId { get; set; }
    public string NombreOriginal { get; set; } = "";
    public string NombreArchivo { get; set; } = "";
    public string TipoMime { get; set; } = "";
    public bool EsImagen { get; set; }
    public string SubidoPor { get; set; } = "";
    public DateTime FechaSubida { get; set; } = DateTime.Now;
    public Tarea? Tarea { get; set; }
}
