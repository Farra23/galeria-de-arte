using FluentAssertions;
using Galeria.Domain.Entities;

namespace Galeria.Tests.Obras;

public class ObraCalcularPrecioVentaTests
{
    [Fact]
    public void Sin_iva_aplica_solo_la_utilidad()
    {
        var precio = Obra.CalcularPrecioVenta(costo: 1000, utilidad: 50, tieneIva: false, ivaPorcentaje: 22, redondeo: 1);

        precio.Should().Be(1500); // 1000 * 1.50
    }

    [Fact]
    public void Con_iva_lo_suma_antes_de_aplicar_la_utilidad()
    {
        var precio = Obra.CalcularPrecioVenta(costo: 1000, utilidad: 50, tieneIva: true, ivaPorcentaje: 22, redondeo: 1);

        precio.Should().Be(1830); // 1000 * 1.22 * 1.50
    }

    [Fact]
    public void Redondea_al_multiplo_mas_cercano_como_en_pesos()
    {
        // 1000 * 1.22 * 1.15 = 1403 -> redondeado a múltiplo de 10 => 1400
        var precio = Obra.CalcularPrecioVenta(costo: 1000, utilidad: 15, tieneIva: true, ivaPorcentaje: 22, redondeo: 10);

        precio.Should().Be(1400);
    }

    [Fact]
    public void Redondeo_cero_o_negativo_deja_el_precio_bruto_sin_redondear()
    {
        var precio = Obra.CalcularPrecioVenta(costo: 100, utilidad: 33, tieneIva: false, ivaPorcentaje: 22, redondeo: 0);

        precio.Should().Be(133);
    }

    [Fact]
    public void La_version_de_instancia_usa_los_datos_propios_de_la_obra()
    {
        var obra = new Obra { Costo = 1000, Utilidad = 50, TieneIVA = false };

        obra.CalcularPrecioVenta(ivaPorcentaje: 22, redondeo: 1).Should().Be(1500);
    }
}
