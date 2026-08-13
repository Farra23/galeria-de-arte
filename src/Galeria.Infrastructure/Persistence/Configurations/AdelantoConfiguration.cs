using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galeria.Infrastructure.Persistence.Configurations;

public class AdelantoConfiguration : IEntityTypeConfiguration<Adelanto>
{
    public void Configure(EntityTypeBuilder<Adelanto> builder)
    {
        builder.Property(a => a.Observaciones).HasMaxLength(500);

        builder.HasOne(a => a.Artista)
            .WithMany(ar => ar.Adelantos)
            .HasForeignKey(a => a.ArtistaId);

        // Opcional: null mientras el adelanto no fue descontado en ninguna liquidación (ver FueDescontado).
        builder.HasOne(a => a.Liquidacion)
            .WithMany(l => l.Adelantos)
            .HasForeignKey(a => a.LiquidacionId);

        builder.HasIndex(a => a.ArtistaId);
    }
}
