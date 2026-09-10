using Galeria.Domain.Entities;
using Galeria.Domain.Enums;

namespace Galeria.DataImport.Importadores;

/// <summary>
/// Hoja "Adelantos" de Liquidaciones.xlsm. Solo se importan los que **todavía no fueron
/// descontados** (columna "Fecha Descontado" vacía): esos afectan el saldo actual del artista y
/// tienen que quedar exactos. Los ya descontados son historia que se resuelve con la liquidación
/// de apertura.
/// </summary>
public sealed class AdelantosImportador : IImportador
{
    public string Nombre => "Adelantos sin descontar";

    private const int ColFecha = 2, ColCodArtista = 3, ColArtista = 4, ColImporte = 5,
        ColMoneda = 6, ColFechaDescontado = 7;

    public async Task EjecutarAsync(Fuentes fuentes, Contexto contexto, Informe informe)
    {
        if (fuentes.Liquidaciones is null)
        {
            informe.Aviso("No se encontró Liquidaciones.xlsm — no se importaron adelantos.");
            return;
        }

        var importados = 0;
        foreach (var fila in fuentes.Liquidaciones.Filas("Adelantos", filasEncabezado: 2))
        {
            if (fila.Fecha(ColFechaDescontado) is not null || fila.Texto(ColFechaDescontado) is not null)
            {
                continue; // ya descontado
            }

            var importe = fila.Decimal(ColImporte);
            if (importe is null or 0)
            {
                continue;
            }

            var artistaId = contexto.ResolverArtista(fila.Entero(ColCodArtista), fila.Texto(ColArtista));
            if (artistaId is null)
            {
                informe.Rechazo(Nombre, "artista no encontrado");
                continue;
            }

            var fecha = fila.Fecha(ColFecha) ?? DateOnly.FromDateTime(DateTime.Today);

            contexto.Db.Adelantos.Add(new Adelanto
            {
                ArtistaId = artistaId.Value,
                Fecha = fecha,
                Importe = Math.Abs(importe.Value),
                Moneda = LeerMoneda(fila.Texto(ColMoneda)) ?? Moneda.Pesos,
                Tipo = importe.Value < 0 ? TipoAdelanto.AjusteAFavor : TipoAdelanto.Adelanto,
                Observaciones = "Adelanto importado del Excel de la galería.",
                FechaDescontado = null,
                LiquidacionId = null,
            });
            importados++;
        }

        await contexto.Db.SaveChangesAsync();
        contexto.Db.ChangeTracker.Clear();
        informe.Importados(Nombre, importados);
    }

    private static Moneda? LeerMoneda(string? texto) => Texto.Clave(texto) switch
    {
        "pesos" or "peso" or "uyu" => Moneda.Pesos,
        "dolares" or "dolar" or "usd" => Moneda.USD,
        _ => null
    };
}
