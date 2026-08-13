using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galeria.Infrastructure.Persistence.Configurations;

public class AlquilerConfiguration : IEntityTypeConfiguration<Alquiler>
{
    public void Configure(EntityTypeBuilder<Alquiler> builder)
    {
        builder.Property(a => a.Cliente).HasMaxLength(200);

        builder.HasOne(a => a.Obra)
            .WithMany(o => o.Alquileres)
            .HasForeignKey(a => a.ObraId);

        builder.HasIndex(a => a.ObraId);
    }
}
