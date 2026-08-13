using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galeria.Infrastructure.Persistence.Configurations;

public class AuditoriaConfiguration : IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> builder)
    {
        builder.Property(a => a.UsuarioId).IsRequired().HasMaxLength(450); // largo estándar de ASP.NET Identity
        builder.Property(a => a.NombreUsuario).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Pantalla).IsRequired().HasMaxLength(100);
        builder.Property(a => a.TipoOperacion).IsRequired().HasMaxLength(30);
        builder.Property(a => a.Tabla).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Columna).HasMaxLength(100);
        builder.Property(a => a.ValorAnterior).HasMaxLength(1000);
        builder.Property(a => a.ValorNuevo).HasMaxLength(1000);
        builder.Property(a => a.EntidadId).HasMaxLength(50);

        // ArtistaId/ObraId son de referencia libre, no FK reales: Auditoria registra cambios
        // de cualquier tabla del sistema, no solo de Artista u Obra.
        //
        // El sistema origen ya tiene 130.352 registros de auditoría (docs/CONTEXTO.md, hallazgo 4) —
        // sin estos índices, cualquier pantalla de "historial de esta obra/artista" hace table scan.
        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => new { a.Tabla, a.EntidadId });
        builder.HasIndex(a => a.ArtistaId);
        builder.HasIndex(a => a.ObraId);
    }
}
