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

        // Valores de negocio ya decididos (docs/CONTEXTO.md, sección 4). Los datos de la galería
        // (nombre, dirección, logo, plantillas) no se siembran acá: son específicos del cliente
        // y se van a cargar desde la pantalla de Configuración cuando exista.
        builder.HasData(
            new Parametro { Clave = Parametro.Claves.IvaPorcentaje, Valor = "22" },
            new Parametro { Clave = Parametro.Claves.RedondeosPesos, Valor = "10" },
            new Parametro { Clave = Parametro.Claves.RedondeoDolar, Valor = "1" },
            new Parametro { Clave = Parametro.Claves.UtilidadDefault, Valor = "50" });
    }
}
