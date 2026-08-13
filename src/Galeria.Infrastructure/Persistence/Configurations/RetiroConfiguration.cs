using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galeria.Infrastructure.Persistence.Configurations;

public class RetiroConfiguration : IEntityTypeConfiguration<Retiro>
{
    public void Configure(EntityTypeBuilder<Retiro> builder)
    {
        builder.Property(r => r.Motivo).HasMaxLength(500);

        builder.HasOne(r => r.Obra)
            .WithMany(o => o.Retiros)
            .HasForeignKey(r => r.ObraId);

        builder.HasIndex(r => r.ObraId);
    }
}
