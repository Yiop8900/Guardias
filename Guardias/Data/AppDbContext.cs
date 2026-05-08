using Microsoft.EntityFrameworkCore;
using Guardias.Models;

namespace Guardias.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Edificio> Edificios { get; set; }
    public DbSet<Guardia> Guardias { get; set; }
    public DbSet<Ronda> Rondas { get; set; }
    public DbSet<FotoRonda> FotosRonda { get; set; }
    public DbSet<Incidencia> Incidencias { get; set; }
    public DbSet<Tarea> Tareas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Edificio>().HasData(
            new Edificio { Id = 1, Nombre = "Torre A", Descripcion = "Torre principal", Activo = true },
            new Edificio { Id = 2, Nombre = "Torre B", Descripcion = "Torre secundaria", Activo = true }
        );
    }
}
