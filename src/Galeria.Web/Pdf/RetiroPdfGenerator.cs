using Galeria.Application.Retiros;
using Galeria.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Galeria.Web.Pdf;

// Requerimiento 6.2: "Envío por mail o teléfono de un PDF con los datos del retiro, e
// imprimible" — hasta ahora el retiro no tenía ninguna versión de documento.
public static class RetiroPdfGenerator
{
    public static byte[] Generar(RetiroListItem retiro, string nombreGaleria)
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
                    col.Item().Text(retiro.Tipo == TipoRetiro.Temporal ? "Retiro temporal" : "Retiro definitivo").FontSize(18).Bold();
                    col.Item().Text(nombreGaleria).FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(4).Text($"Fecha: {retiro.Fecha:dd/MM/yyyy}").FontSize(9);
                });

                pagina.Content().PaddingTop(15).Column(col =>
                {
                    col.Spacing(6);
                    Fila(col, "Código", retiro.CodigoObra);
                    Fila(col, "Obra", retiro.Titulo);
                    Fila(col, "Artista", retiro.ArtistaNombre);
                    Fila(col, "Motivo", retiro.Motivo ?? "—");

                    if (retiro.Tipo == TipoRetiro.Temporal)
                    {
                        Fila(col, "Devolución estimada", retiro.FechaEstimadaDevolucion?.ToString("dd/MM/yyyy") ?? "—");
                        Fila(col, "Estado", retiro.EstaDevuelto ? "Devuelto" : "Pendiente de devolución");
                    }
                    else
                    {
                        col.Item().PaddingTop(10).Text("La pieza queda retirada del stock en forma definitiva.").Italic();
                    }
                });

                pagina.Footer().AlignCenter().Text(x => x.CurrentPageNumber());
            });
        }).GeneratePdf();
    }

    private static void Fila(ColumnDescriptor columna, string etiqueta, string valor) =>
        columna.Item().Row(fila =>
        {
            fila.ConstantItem(120).Text(etiqueta).SemiBold();
            fila.RelativeItem().Text(valor);
        });
}
