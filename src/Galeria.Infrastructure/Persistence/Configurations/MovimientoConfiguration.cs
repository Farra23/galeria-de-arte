using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galeria.Infrastructure.Persistence.Configurations;

public class MovimientoConfiguration : IEntityTypeConfiguration<Movimiento>
{
    public void Configure(EntityTypeBuilder<Movimiento> builder)
    {
        builder.HasOne(m => m.Obra)
            .WithMany(o => o.Movimientos)
            .HasForeignKey(m => m.ObraId);

        // El libro de stock se reconstruye leyendo los movimientos de una obra en orden de fecha
        // (ver comentario en la entidad Movimiento) — este índice es lo que sostiene esa consulta.
        builder.HasIndex(m => new { m.ObraId, m.Fecha });
    }
}
