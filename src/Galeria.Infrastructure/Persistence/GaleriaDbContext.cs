using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence;

// Separado a propósito del ApplicationDbContext de Identity (que vive en Galeria.Web):
// la autenticación no es parte del dominio de la galería. Dos DbContext, dos responsabilidades (SRP).
public class GaleriaDbContext(DbContextOptions<GaleriaDbContext> options) : DbContext(options)
{
    public DbSet<Artista> Artistas => Set<Artista>();
    public DbSet<Obra> Obras => Set<Obra>();
    public DbSet<Serie> Series => Set<Serie>();
    public DbSet<Rubro> Rubros => Set<Rubro>();
    public DbSet<Tecnica> Tecnicas => Set<Tecnica>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();
    public DbSet<Retiro> Retiros => Set<Retiro>();
    public DbSet<Alquiler> Alquileres => Set<Alquiler>();
    public DbSet<Devolucion> Devoluciones => Set<Devolucion>();
    public DbSet<Adelanto> Adelantos => Set<Adelanto>();
    public DbSet<Liquidacion> Liquidaciones => Set<Liquidacion>();
    public DbSet<LineaLiquidacion> LineasLiquidacion => Set<LineaLiquidacion>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<Certificado> Certificados => Set<Certificado>();
    public DbSet<Parametro> Parametros => Set<Parametro>();
    public DbSet<AgendaPago> AgendasPago => Set<AgendaPago>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // DRY: precisión decimal de todos los montos en un solo lugar, en vez de repetir
        // HasPrecision en cada propiedad decimal de cada Configuration (son ~20 en total).
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Cada entidad tiene su propia clase XConfiguration : IEntityTypeConfiguration<X>
        // (patrón de EF Core, aplica SRP: el DbContext no sabe los detalles de mapeo de cada tabla).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GaleriaDbContext).Assembly);

        // Regla de negocio (decisión #9, docs/CONTEXTO.md): nunca se borra nada, se marca
        // (Activo, Estado, FechaDevolucion, etc.). Se fuerza acá, a nivel de infraestructura,
        // para que ninguna relación futura pueda reintroducir un cascade delete por accidente
        // y arrastrarse ventas, liquidaciones o movimientos históricos.
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
