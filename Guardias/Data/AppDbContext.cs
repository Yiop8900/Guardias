using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
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
    public DbSet<Area> Areas { get; set; }
    public DbSet<AreaRonda> AreaRondas { get; set; }
    public DbSet<UsuarioEdificio> UsuariosEdificio { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Guardia>()
            .HasOne(g => g.Edificio)
            .WithMany(e => e.Guardias)
            .HasForeignKey(g => g.EdificioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ronda>()
            .HasOne(r => r.Guardia)
            .WithMany(g => g.Rondas)
            .HasForeignKey(r => r.GuardiaId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Ronda>()
            .HasOne(r => r.Edificio)
            .WithMany(e => e.Rondas)
            .HasForeignKey(r => r.EdificioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Tarea>()
            .HasOne(t => t.Guardia)
            .WithMany(g => g.Tareas)
            .HasForeignKey(t => t.GuardiaId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Tarea>()
            .HasOne(t => t.Edificio)
            .WithMany(e => e.Tareas)
            .HasForeignKey(t => t.EdificioId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Area>()
            .HasOne(a => a.Edificio)
            .WithMany(e => e.Areas)
            .HasForeignKey(a => a.EdificioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AreaRonda>()
            .HasOne(ar => ar.Ronda)
            .WithMany(r => r.AreaRondas)
            .HasForeignKey(ar => ar.RondaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AreaRonda>()
            .HasOne(ar => ar.Area)
            .WithMany(a => a.AreaRondas)
            .HasForeignKey(ar => ar.AreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FotoRonda>()
            .HasOne(f => f.AreaRonda)
            .WithMany(ar => ar.Fotos)
            .HasForeignKey(f => f.AreaRondaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Incidencia>()
            .HasOne(i => i.AreaRonda)
            .WithMany(ar => ar.Incidencias)
            .HasForeignKey(i => i.AreaRondaId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        modelBuilder.Entity<UsuarioEdificio>()
            .HasOne(u => u.Edificio)
            .WithMany(e => e.UsuariosEdificio)
            .HasForeignKey(u => u.EdificioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UsuarioEdificio>()
            .HasIndex(u => u.NombreUsuario)
            .IsUnique();

    }

    // Seed dynamic data — called at startup after migration (nothing to seed by default)
    public static Task SeedAsync(AppDbContext db) => Task.CompletedTask;
}