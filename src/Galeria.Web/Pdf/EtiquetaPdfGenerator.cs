using Galeria.Application.Obras;
using Galeria.Web;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp.Rendering;

namespace Galeria.Web.Pdf;

// Adenda 1 al contrato (etiquetas adhesivas con código de barras): una etiqueta individual de
// 20x30mm con código de barras, código de la obra y precio. Se manda a imprimir por el diálogo
// estándar de Windows contra la impresora de etiquetas que el cliente ya tiene — no se programa
// contra comandos directos (ZPL/EPL), ver Adenda 1 punto 1.
public static class EtiquetaPdfGenerator
{
    // Tamaño real de la etiqueta física (Adenda 1: 20x30mm) menos un margen de seguridad interno
    // — pedido explícito de no imprimir recuadros y dejar aire, porque el desvío mecánico de la
    // impresora puede cortar contenido pegado al borde.
    private const float AnchoMm = 30f;
    private const float AltoMm = 20f;

    public static byte[] Generar(ObraFicha obra)
    {
        var codigoBarras = GenerarCodigoBarras(obra.CodigoVisible);

        return Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(AnchoMm, AltoMm, Unit.Millimetre);
                pagina.Margin(1, Unit.Millimetre);
                pagina.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(6));

                pagina.Content().Column(col =>
                {
                    col.Spacing(0.5f, Unit.Millimetre);

                    col.Item().AlignCenter().Height(9, Unit.Millimetre).Image(codigoBarras).FitArea();
                    col.Item().AlignCenter().Text(obra.CodigoVisible).FontSize(7).Bold();
                    col.Item().AlignCenter().Text(Formato.Monto(obra.PrecioVenta, obra.Moneda)).FontSize(8).Bold();
                });
            });
        }).GeneratePdf();
    }

    // Code128 soporta el código numérico de la obra sin necesidad de dígito de control extra ni
    // esquema especial — cualquier lector de código de barras USB genérico lo lee de fábrica.
    private static byte[] GenerarCodigoBarras(string codigo)
    {
        var writer = new BarcodeWriter<SKBitmap>
        {
            Format = BarcodeFormat.CODE_128,
            Renderer = new SKBitmapRenderer(),
            Options = new EncodingOptions
            {
                Width = 300,
                Height = 100,
                Margin = 0,
                PureBarcode = true
            }
        };

        using var bitmap = writer.Write(codigo);
        using var imagen = SKImage.FromBitmap(bitmap);
        using var datos = imagen.Encode(SKEncodedImageFormat.Png, 100);
        return datos.ToArray();
    }
}
