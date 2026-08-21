using FluentAssertions;
using Galeria.Application.Alquileres;

namespace Galeria.Tests.Alquileres;

public class AlquilerServiceTests
{
    [Fact]
    public void Ejemplo_del_requerimiento_7_2()
    {
        // Obra de 1.000, alquiler al 10% → 100. Reparto 50/50 → 50 y 50.
        var (montoAlquiler, montoArtista, montoGaleria) = AlquilerService.CalcularReparto(1000, 10, 50);

        montoAlquiler.Should().Be(100);
        montoArtista.Should().Be(50);
        montoGaleria.Should().Be(50);
    }

    [Fact]
    public void El_reparto_del_artista_y_la_galeria_siempre_suman_el_total_del_alquiler()
    {
        var (montoAlquiler, montoArtista, montoGaleria) = AlquilerService.CalcularReparto(1234.56m, 12.5m, 37);

        (montoArtista + montoGaleria).Should().Be(montoAlquiler);
    }

    [Fact]
    public void Cien_por_ciento_para_el_artista_no_deja_nada_para_la_galeria()
    {
        var (montoAlquiler, montoArtista, montoGaleria) = AlquilerService.CalcularReparto(1000, 10, 100);

        montoArtista.Should().Be(montoAlquiler);
        montoGaleria.Should().Be(0);
    }

    [Fact]
    public void Cero_por_ciento_para_el_artista_deja_todo_para_la_galeria()
    {
        var (montoAlquiler, montoArtista, montoGaleria) = AlquilerService.CalcularReparto(1000, 10, 0);

        montoArtista.Should().Be(0);
        montoGaleria.Should().Be(montoAlquiler);
    }
}
