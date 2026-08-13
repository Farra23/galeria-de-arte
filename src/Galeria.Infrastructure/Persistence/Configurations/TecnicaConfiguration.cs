using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galeria.Infrastructure.Persistence.Configurations;

public class TecnicaConfiguration : IEntityTypeConfiguration<Tecnica>
{
    public void Configure(EntityTypeBuilder<Tecnica> builder)
    {
        builder.Property(t => t.Nombre).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Nombre).IsUnique();
    }
}
