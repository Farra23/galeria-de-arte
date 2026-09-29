using FluentAssertions;
using Galeria.Application.Obras;

namespace Galeria.Tests.Obras;

// Punto 18 del testeo de cliente: buscar una obra por el código de la etiqueta en vez del título
// exacto. Parsear() es lógica pura (sin base de datos) a propósito, para poder cubrir los casos
// borde del formato sin necesitar un contexto de EF Core.
public class CodigoObraParserTests
{
    [Fact]
    public void Codigo_de_seis_digitos_separa_los_ultimos_tres_como_numero_de_obra()
    {
        CodigoObraParser.Parsear("025014").Should().Be((25, 14));
    }

    [Fact]
    public void Funciona_igual_sin_ceros_a_la_izquierda_en_la_parte_del_artista()
    {
        CodigoObraParser.Parsear("25014").Should().Be((25, 14));
    }

    [Fact]
    public void Ignora_separadores_como_guiones_o_espacios_del_copy_paste()
    {
        CodigoObraParser.Parsear("025-014").Should().Be((25, 14));
        CodigoObraParser.Parsear(" 025 014 ").Should().Be((25, 14));
    }

    [Fact]
    public void Codigo_de_artista_con_cuatro_digitos_tambien_separa_bien()
    {
        // Caso del punto 21 (bug de numeración): un artista con código de 4+ dígitos.
        CodigoObraParser.Parsear("1000014").Should().Be((1000, 14));
    }

    [Fact]
    public void Tres_digitos_o_menos_es_ambiguo_y_no_se_reconoce()
    {
        CodigoObraParser.Parsear("014").Should().BeNull();
        CodigoObraParser.Parsear("1").Should().BeNull();
    }

    [Fact]
    public void Vacio_o_solo_espacios_no_se_reconoce()
    {
        CodigoObraParser.Parsear("").Should().BeNull();
        CodigoObraParser.Parsear("   ").Should().BeNull();
        CodigoObraParser.Parsear(null).Should().BeNull();
    }

    [Fact]
    public void Texto_sin_digitos_no_se_reconoce()
    {
        CodigoObraParser.Parsear("Sin título").Should().BeNull();
    }

    [Fact]
    public void Titulo_que_por_casualidad_tiene_pocos_digitos_no_se_confunde_con_un_codigo()
    {
        // "Serie 2" no debería interpretarse como código -- solo tiene 1 dígito.
        CodigoObraParser.Parsear("Serie 2").Should().BeNull();
    }

    [Fact]
    public void Numero_demasiado_largo_para_un_int_no_explota_devuelve_null()
    {
        CodigoObraParser.Parsear("99999999999999999999014").Should().BeNull();
    }
}
