using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galeria.Infrastructure.Persistence.Configurations;

public class CertificadoConfiguration : IEntityTypeConfiguration<Certificado>
{
    public void Configure(EntityTypeBuilder<Certificado> builder)
    {
        builder.Property(c => c.EmailDestinatario).HasMaxLength(200);
        builder.Property(c => c.PdfPath).HasMaxLength(500);

        // Relación 1:1 — una venta tiene a lo sumo un certificado.
        builder.HasOne(c => c.Venta)
            .WithOne(v => v.Certificado)
            .HasForeignKey<Certificado>(c => c.VentaId);

        builder.HasIndex(c => c.VentaId).IsUnique();
    }
}
