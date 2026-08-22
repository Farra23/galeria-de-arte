using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galeria.Infrastructure.Persistence.Configurations;

public class AgendaPagoConfiguration : IEntityTypeConfiguration<AgendaPago>
{
    public void Configure(EntityTypeBuilder<AgendaPago> builder)
    {
        builder.Property(a => a.Comentarios).HasMaxLength(500);

        builder.HasOne(a => a.Artista)
            .WithMany()
            .HasForeignKey(a => a.ArtistaId);

        // Un solo registro de seguimiento por artista (ver comentario en la entidad).
        builder.HasIndex(a => a.ArtistaId).IsUnique();
    }
}
