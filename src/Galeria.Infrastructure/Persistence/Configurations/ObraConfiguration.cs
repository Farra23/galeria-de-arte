using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galeria.Infrastructure.Persistence.Configurations;

public class ObraConfiguration : IEntityTypeConfiguration<Obra>
{
    public void Configure(EntityTypeBuilder<Obra> builder)
    {
        builder.Property(o => o.Titulo).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Observaciones).HasMaxLength(2000);
        builder.Property(o => o.ImagenPrincipalPath).HasMaxLength(500);

        // Decisión #1: el correlativo es por artista → el par (ArtistaId, NumeroObra)
        // es la verdadera clave de negocio, aunque la PK real sea el Id autonumérico.
        builder.HasIndex(o => new { o.ArtistaId, o.NumeroObra }).IsUnique();

        // Decisión #6: detección de obra duplicada por nombre dentro del mismo artista,
        // vía autocompletado al tipear. Este índice es lo que hace esa búsqueda rápida.
        builder.HasIndex(o => new { o.ArtistaId, o.Titulo });

        builder.HasOne(o => o.Serie)
            .WithMany(s => s.Obras)
            .HasForeignKey(o => o.SerieId);

        builder.HasOne(o => o.Rubro)
            .WithMany(r => r.Obras)
            .HasForeignKey(o => o.RubroId);

        builder.HasOne(o => o.Tecnica)
            .WithMany(t => t.Obras)
            .HasForeignKey(o => o.TecnicaId);
    }
}
