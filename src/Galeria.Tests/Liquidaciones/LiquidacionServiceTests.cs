using FluentAssertions;
using Galeria.Application.Liquidaciones;
using Galeria.Domain.Enums;

namespace Galeria.Tests.Liquidaciones;

// El motor de liquidación es la pieza con más lógica de negocio de todo el ERP (docs/CONTEXTO.md
// lo señala como lo que más rinde para portfolio) — CalcularTotales es un método puro, sin base
// de datos, pensado justamente para poder testearlo así.
public class LiquidacionServiceTests
{
    private static LineaPendiente Venta(decimal montoTotal) =>
        new(TipoLinea.Venta, new DateOnly(2026, 1, 1), 1, "001001", "Obra", 1, montoTotal, montoTotal, null, 1);

    private static LineaPendiente Alquiler(decimal montoTotal) =>
        new(TipoLinea.Alquiler, new DateOnly(2026, 1, 1), 1, "001001", "Obra", 1, montoTotal, montoTotal, null, 1);

    private static LineaPendiente PagoContado() =>
        new(TipoLinea.PagoContado, new DateOnly(2026, 1, 1), 1, "001001", "Obra", 1, 0, 0, "Pago contado", 1);

    private static LineaPendiente Devolucion() =>
        new(TipoLinea.Devolucion, new DateOnly(2026, 1, 1), 1, "001001", "Obra", 1, 0, 0, "Devuelta", 1);

    // Adelanto ya viene con el signo aplicado (Adelanto.ImpactoEnLiquidacion en el dominio):
    // negativo para Adelanto, positivo para AjusteAFavor.
    private static LineaPendiente Adelanto(decimal montoConSigno) =>
        new(TipoLinea.Adelanto, new DateOnly(2026, 1, 1), null, "", "Adelanto", 1, montoConSigno, montoConSigno, null, 1);

    [Fact]
    public void Sin_lineas_todos_los_totales_dan_cero()
    {
        var (bruto, adelantos, devoluciones, neto) = LiquidacionService.CalcularTotales([]);

        bruto.Should().Be(0);
        adelantos.Should().Be(0);
        devoluciones.Should().Be(0);
        neto.Should().Be(0);
    }

    [Fact]
    public void Una_venta_simple_el_neto_es_el_costo_de_la_obra()
    {
        var (bruto, adelantos, devoluciones, neto) = LiquidacionService.CalcularTotales([Venta(1000)]);

        bruto.Should().Be(1000);
        adelantos.Should().Be(0);
        devoluciones.Should().Be(0);
        neto.Should().Be(1000);
    }

    [Fact]
    public void Pago_contado_suma_al_bruto_pero_no_cambia_el_neto_porque_vale_cero()
    {
        var (bruto, _, _, neto) = LiquidacionService.CalcularTotales([Venta(1000), PagoContado()]);

        bruto.Should().Be(1000); // PagoContado aporta 0
        neto.Should().Be(1000);
    }

    [Fact]
    public void Devolucion_no_liquidada_no_suma_ni_resta_plata()
    {
        var (bruto, _, devoluciones, neto) = LiquidacionService.CalcularTotales([Venta(1000), Devolucion()]);

        bruto.Should().Be(1000);
        devoluciones.Should().Be(0);
        neto.Should().Be(1000);
    }

    [Fact]
    public void Adelanto_resta_del_neto()
    {
        var (bruto, adelantos, _, neto) = LiquidacionService.CalcularTotales([Venta(1000), Adelanto(-300)]);

        bruto.Should().Be(1000);
        adelantos.Should().Be(-300);
        neto.Should().Be(700);
    }

    [Fact]
    public void Ajuste_a_favor_suma_al_neto()
    {
        var (_, adelantos, _, neto) = LiquidacionService.CalcularTotales([Venta(1000), Adelanto(200)]);

        adelantos.Should().Be(200);
        neto.Should().Be(1200);
    }

    [Fact]
    public void Ejemplo_del_requerimiento_11_1_arrastre_de_saldo()
    {
        // docs/REQUERIMIENTOS 11.1: en mayo vendió $100 y no cobró, en junio vende $10.000
        // → el total a liquidar junto es $10.100.
        var (bruto, _, _, neto) = LiquidacionService.CalcularTotales([Venta(100), Venta(10000)]);

        bruto.Should().Be(10100);
        neto.Should().Be(10100);
    }

    [Fact]
    public void Combinacion_de_venta_alquiler_adelanto_y_devolucion()
    {
        var lineas = new List<LineaPendiente>
        {
            Venta(1000),
            Alquiler(150),
            Adelanto(-400),
            Devolucion()
        };

        var (bruto, adelantos, devoluciones, neto) = LiquidacionService.CalcularTotales(lineas);

        bruto.Should().Be(1150);
        adelantos.Should().Be(-400);
        devoluciones.Should().Be(0);
        neto.Should().Be(750);
    }
}
