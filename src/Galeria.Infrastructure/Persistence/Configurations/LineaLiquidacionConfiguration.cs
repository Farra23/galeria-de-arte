using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galeria.Infrastructure.Persistence.Configurations;

public class LineaLiquidacionConfiguration : IEntityTypeConfiguration<LineaLiquidacion>
{
    public void Configure(EntityTypeBuilder<LineaLiquidacion> builder)
    {
        // Desnormalizado a propósito (ver comentario en la entidad): el PDF de una liquidación
        // confirmada no puede cambiar si mañana se edita el nombre de la obra en otro lado.
        builder.Property(l => l.CodigoObra).IsRequired().HasMaxLength(20);
        builder.Property(l => l.NombreObra).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Observaciones).HasMaxLength(500);

        builder.HasOne(l => l.Liquidacion)
            .WithMany(liq => liq.Lineas)
            .HasForeignKey(l => l.LiquidacionId);

        builder.HasOne(l => l.Obra)
            .WithMany()
            .HasForeignKey(l => l.ObraId);

        builder.HasIndex(l => l.LiquidacionId);
    }
}
