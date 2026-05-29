using System.ComponentModel.DataAnnotations;

namespace Guardias.Models;

public enum Turno
{
    [Display(Name = "Mañana")] Manana = 0,
    [Display(Name = "Tarde")] Tarde = 1,
    [Display(Name = "Noche")] Noche = 2,
    [Display(Name = "Todos los turnos")] Todos = 3
}

public enum EstadoRonda
{
    [Display(Name = "En curso")] EnCurso = 0,
    [Display(Name = "Finalizada")] Finalizada = 1,
    [Display(Name = "Firmada")] FirmadaSupervisor = 2,
    [Display(Name = "Reporte directo")] ReporteDirecto = 3
}

public enum EstadoIncidencia
{
    [Display(Name = "Abierta")] Abierta = 0,
    [Display(Name = "En proceso")] EnProceso = 1,
    [Display(Name = "Cerrada")] Cerrada = 2
}

public enum SeveridadIncidencia
{
    [Display(Name = "Leve")] Leve = 0,
    [Display(Name = "Moderada")] Moderada = 1,
    [Display(Name = "Grave")] Grave = 2
}

public enum EstadoTarea
{
    [Display(Name = "Pendiente")] Pendiente = 0,
    [Display(Name = "En proceso")] EnProceso = 1,
    [Display(Name = "Completada")] Completada = 2
}
