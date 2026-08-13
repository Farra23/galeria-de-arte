using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galeria.Infrastructure.Persistence.Configurations;

public class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> builder)
    {
        builder.Property(v => v.Observaciones).HasMaxLength(1000);

        builder.HasOne(v => v.Obra)
            .WithMany(o => o.Ventas)
            .HasForeignKey(v => v.ObraId);

        builder.HasIndex(v => v.ObraId);
        builder.HasIndex(v => v.Fecha);
    }
}
