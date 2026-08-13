using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galeria.Infrastructure.Persistence.Configurations;

public class DevolucionConfiguration : IEntityTypeConfiguration<Devolucion>
{
    public void Configure(EntityTypeBuilder<Devolucion> builder)
    {
        builder.Property(d => d.Motivo).HasMaxLength(500);

        // Relación 1:1 — decisión #4: la devolución es una línea propia, no un adelanto disfrazado.
        builder.HasOne(d => d.Venta)
            .WithOne(v => v.Devolucion)
            .HasForeignKey<Devolucion>(d => d.VentaId);

        builder.HasIndex(d => d.VentaId).IsUnique();
    }
}
