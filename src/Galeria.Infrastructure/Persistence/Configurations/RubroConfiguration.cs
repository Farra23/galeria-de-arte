using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galeria.Infrastructure.Persistence.Configurations;

public class RubroConfiguration : IEntityTypeConfiguration<Rubro>
{
    public void Configure(EntityTypeBuilder<Rubro> builder)
    {
        builder.Property(r => r.Nombre).IsRequired().HasMaxLength(100);
        builder.HasIndex(r => r.Nombre).IsUnique();
    }
}
