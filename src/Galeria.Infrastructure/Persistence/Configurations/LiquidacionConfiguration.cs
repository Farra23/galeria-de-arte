using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galeria.Infrastructure.Persistence.Configurations;

public class LiquidacionConfiguration : IEntityTypeConfiguration<Liquidacion>
{
    public void Configure(EntityTypeBuilder<Liquidacion> builder)
    {
        builder.Property(l => l.PdfPath).HasMaxLength(500);

        builder.HasOne(l => l.Artista)
            .WithMany(a => a.Liquidaciones)
            .HasForeignKey(l => l.ArtistaId);

        builder.HasIndex(l => l.ArtistaId);
        builder.HasIndex(l => new { l.PeriodoAnio, l.PeriodoMes });
    }
}
