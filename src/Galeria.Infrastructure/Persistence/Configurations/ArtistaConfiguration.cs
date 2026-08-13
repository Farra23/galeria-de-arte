using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galeria.Infrastructure.Persistence.Configurations;

public class ArtistaConfiguration : IEntityTypeConfiguration<Artista>
{
    public void Configure(EntityTypeBuilder<Artista> builder)
    {
        builder.Property(a => a.Apellido).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Nombre).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Perfil).HasMaxLength(2000);
        builder.Property(a => a.Taller).HasMaxLength(200);
        builder.Property(a => a.Celular).HasMaxLength(30);
        builder.Property(a => a.TelFijo).HasMaxLength(30);
        builder.Property(a => a.Direccion).HasMaxLength(300);
        builder.Property(a => a.Correo).HasMaxLength(200);

        // Decisión #2 (docs/CONTEXTO.md): código de artista automático → único.
        builder.HasIndex(a => a.Codigo).IsUnique();
    }
}
