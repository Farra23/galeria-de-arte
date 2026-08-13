using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galeria.Infrastructure.Persistence.Configurations;

public class ParametroConfiguration : IEntityTypeConfiguration<Parametro>
{
    public void Configure(EntityTypeBuilder<Parametro> builder)
    {
        // Clave-valor: Clave es la PK natural, no hay Id autonumérico (ver Parametro.Claves).
        builder.HasKey(p => p.Clave);
        builder.Property(p => p.Clave).HasMaxLength(100);
        builder.Property(p => p.Valor).IsRequired().HasMaxLength(2000);
    }
}
