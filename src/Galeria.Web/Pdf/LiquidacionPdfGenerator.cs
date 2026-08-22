using Galeria.Application.Liquidaciones;
using Galeria.Domain.Enums;
using Galeria.Web;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Galeria.Web.Pdf;

// Requerimiento 10.3 + 0.2: "tiene que verse profesional", PDF descargable e imprimible, con
// fecha de emisión bien visible y datos de la galería en el encabezado.
public static class LiquidacionPdfGenerator
{
    public static byte[] Generar(LiquidacionDetalle detalle, string nombreGaleria, string? direccionGaleria, string? telefonoGaleria)
    {
        return Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(2, Unit.Centimetre);
                pagina.DefaultTextStyle(x => x.FontSize(10));

                pagina.Header().Row(fila =>
                {
                    fila.RelativeItem().Column(col =>
                    {
                        col.Item().Text(nombreGaleria).FontSize(14).Bold();
                        if (!string.IsNullOrWhiteSpace(direccionGaleria))
                        {
                            col.Item().Text(direccionGaleria).FontSize(9).FontColor(Colors.Grey.Darken1);
                        }
                        if (!string.IsNullOrWhiteSpace(telefonoGaleria))
                        {
                            col.Item().Text(telefonoGaleria).FontSize(9).FontColor(Colors.Grey.Darken1);
                        }
                        col.Item().PaddingTop(2).Text("Comprobante de liquidación a artista").FontSize(9).Italic();
                    });

                    fila.ConstantItem(180).AlignRight().Column(col =>
                    {
                        col.Item().Text($"Liquidación N.º {detalle.NumeroCorrelativo:D6}").FontSize(14).Bold();
                        col.Item().Text($"Fecha de emisión: {detalle.Fecha:dd/MM/yyyy}").FontSize(10).Bold();
                    });
                });

                pagina.Content().PaddingTop(15).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Text($"Artista: {detalle.ArtistaNombre}    Moneda: {Formato.Simbolo(detalle.Moneda)}").Bold();

                    col.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(columnas =>
                        {
                            columnas.ConstantColumn(60);  // fecha
                            columnas.ConstantColumn(60);  // tipo
                            columnas.ConstantColumn(55);  // código
                            columnas.RelativeColumn();    // obra
                            columnas.ConstantColumn(30);  // cant
                            columnas.ConstantColumn(70);  // monto
                            columnas.RelativeColumn(1.3f); // detalle
                        });

                        tabla.Header(header =>
                        {
                            EncabezadoCelda(header, "Fecha");
                            EncabezadoCelda(header, "Tipo");
                            EncabezadoCelda(header, "Código");
                            EncabezadoCelda(header, "Obra");
                            EncabezadoCelda(header, "Cant.");
                            EncabezadoCelda(header, "Monto");
                            EncabezadoCelda(header, "Detalle");
                        });

                        foreach (var linea in detalle.Lineas)
                        {
                            Celda(tabla, linea.Fecha.ToString("dd/MM/yyyy"));
                            Celda(tabla, TipoTexto(linea.Tipo));
                            Celda(tabla, linea.CodigoObra);
                            Celda(tabla, linea.NombreObra);
                            Celda(tabla, linea.Cantidad.ToString());
                            Celda(tabla, Formato.Monto(linea.MontoTotal, detalle.Moneda));
                            Celda(tabla, linea.Observaciones ?? "");
                        }
                    });

                    col.Item().PaddingTop(10).AlignRight().Column(totales =>
                    {
                        totales.Item().Text($"Total bruto: {Formato.Monto(detalle.TotalBruto, detalle.Moneda)}");
                        totales.Item().Text($"Adelantos: {Formato.Monto(detalle.TotalAdelantos, detalle.Moneda)}");
                        totales.Item().PaddingTop(4).Text($"Total neto pagado: {Formato.Monto(detalle.TotalNeto, detalle.Moneda)}").FontSize(13).Bold();
                    });

                    col.Item().PaddingTop(15).Text(
                        "Esta liquidación deja constancia de las piezas vendidas, alquiladas o adelantadas que se pagaron en esta fecha.")
                        .FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
                });

                pagina.Footer().AlignCenter().Text(x => x.CurrentPageNumber());
            });
        }).GeneratePdf();
    }

    private static void EncabezadoCelda(TableCellDescriptor header, string texto) =>
        header.Cell().Element(c => c.BorderBottom(1).BorderColor(Colors.Grey.Medium).PaddingBottom(3)).Text(texto).SemiBold();

    private static void Celda(TableDescriptor tabla, string texto) =>
        tabla.Cell().Element(c => c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3)).Text(texto).FontSize(9);

    private static string TipoTexto(TipoLinea tipo) => tipo switch
    {
        TipoLinea.Venta => "Venta",
        TipoLinea.Alquiler => "Alquiler",
        TipoLinea.PagoContado => "Pago contado",
        TipoLinea.Adelanto => "Adelanto",
        TipoLinea.Devolucion => "Devolución",
        _ => tipo.ToString()
    };
}
