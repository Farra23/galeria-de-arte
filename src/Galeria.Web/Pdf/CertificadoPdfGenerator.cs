using Galeria.Application.Certificados;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Galeria.Web.Pdf;

// Requerimiento 5.3 + 0.2: PDF real del certificado, sin costo ni utilidad (eso ya lo garantiza
// CertificadoDatos, que nunca trae esos campos).
public static class CertificadoPdfGenerator
{
    public static byte[] Generar(CertificadoDatos datos, string nombreGaleria)
    {
        return Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A5);
                pagina.Margin(2, Unit.Centimetre);
                pagina.DefaultTextStyle(x => x.FontSize(11));

                pagina.Header().Column(col =>
                {
                    col.Item().Text("Certificado de autenticidad").FontSize(18).Bold();
                    col.Item().Text(nombreGaleria).FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(4).Text($"N.º {datos.NumeroCertificado:D6} — emitido el {datos.FechaEmision:dd/MM/yyyy}").FontSize(9);
                });

                pagina.Content().PaddingTop(15).Column(col =>
                {
                    col.Spacing(6);
                    Fila(col, "Código", datos.CodigoObra);
                    Fila(col, "Obra", datos.Titulo);
                    Fila(col, "Artista", datos.ArtistaNombre);
                    Fila(col, "Técnica", datos.Tecnica ?? "—");
                    Fila(col, "Rubro", datos.Rubro ?? "—");
                    Fila(col, "Medidas", Medidas(datos));
                    Fila(col, "Año", datos.AnioIngreso.ToString());

                    col.Item().PaddingTop(20).Text(
                        "La galería certifica que la obra descripta en este documento es una pieza original, " +
                        "y garantiza la autenticidad de los datos consignados.").Italic();
                });

                pagina.Footer().AlignCenter().Text(x => x.CurrentPageNumber());
            });
        }).GeneratePdf();
    }

    private static void Fila(ColumnDescriptor columna, string etiqueta, string valor) =>
        columna.Item().Row(fila =>
        {
            fila.ConstantItem(90).Text(etiqueta).SemiBold();
            fila.RelativeItem().Text(valor);
        });

    private static string Medidas(CertificadoDatos datos)
    {
        if (datos.AltoCm is null && datos.AnchoCm is null && datos.LargoCm is null)
        {
            return "—";
        }

        return $"{Formatear(datos.AltoCm)} x {Formatear(datos.AnchoCm)} x {Formatear(datos.LargoCm)} cm";
    }

    private static string Formatear(decimal? valor) => valor?.ToString("0.##") ?? "—";
}
