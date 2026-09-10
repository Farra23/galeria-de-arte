using Galeria.Domain.Entities;
using Galeria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Galeria.DataImport.Importadores;

/// <summary>
/// Hoja "Ventas" (~14.100 filas): toda la historia de ventas. Se importan completas para que la
/// ficha del artista y las listas muestren el histórico; qué ventas quedan pendientes de pagar lo
/// resuelve después <see cref="AperturaImportador"/>.
///
/// Muchas ventas viejas (2018-2020) apuntan a obras que ya no están en el catálogo actual (se
/// renumeraron o se dieron de baja). Para no perder esa historia se crea una obra "fantasma"
/// (existencia 0, sin stock) con los datos que trae la propia venta.
/// </summary>
public sealed class VentasImportador : IImportador
{
    public string Nombre => "Ventas";

    private const int ColFecha = 2, ColCodArtista = 4, ColCodObra = 5, ColArtista = 6,
        ColObra = 7, ColCantidad = 8, ColIva = 9, ColMoneda = 11, ColCosto = 12, ColPrecio = 13, ColObs = 14;

    private const int TamañoLote = 3000;

    public async Task EjecutarAsync(Fuentes fuentes, Contexto contexto, Informe informe)
    {
        var fantasmas = await CrearObrasFantasmaAsync(fuentes, contexto);
        var monedasObra = await CargarMonedasObraAsync(contexto);

        var pendientes = new List<Venta>(TamañoLote);
        var importadas = 0;
        var sinFecha = 0;

        foreach (var fila in fuentes.Principal.Filas("Ventas", filasEncabezado: 2))
        {
            var codArtista = fila.Entero(ColCodArtista);
            var codObra = fila.Entero(ColCodObra);
            if (codArtista is null || codObra is null)
            {
                informe.Rechazo(Nombre, "venta sin código de obra");
                continue;
            }

            var clave = (codArtista.Value, codObra.Value);
            if (!contexto.ObraPorCodigo.TryGetValue(clave, out var obraId) && !fantasmas.TryGetValue(clave, out obraId))
            {
                informe.Rechazo(Nombre, "obra no encontrada y sin datos para crear una fantasma");
                continue;
            }

            var fecha = fila.Fecha(ColFecha);
            if (fecha is null || fecha.Value.Year < 2000)
            {
                sinFecha++;
                informe.Rechazo(Nombre, "venta con fecha inválida o imposible");
                continue;
            }

            if (fecha.Value > DateOnly.FromDateTime(DateTime.Today).AddDays(2))
            {
                sinFecha++;
                informe.Rechazo(Nombre, "venta con fecha futura (año mal tipeado en el Excel)");
                continue;
            }

            var precio = Math.Max(0, fila.Decimal(ColPrecio) ?? 0);
            var moneda = LeerMoneda(fila.Texto(ColMoneda)) ?? monedasObra.GetValueOrDefault(obraId, Moneda.Pesos);

            pendientes.Add(new Venta
            {
                ObraId = obraId,
                Fecha = fecha.Value,
                Cantidad = Math.Max(1, fila.Entero(ColCantidad) ?? 1),
                Moneda = moneda,
                PrecioVenta = precio,
                PrecioCalculado = precio, // el desvío histórico no se conserva: sin base para calcularlo
                ExentaIVA = (fila.Decimal(ColIva) ?? 0) == 0,
                Observaciones = Recortar(fila.Texto(ColObs), 1000),
            });
            importadas++;

            if (pendientes.Count >= TamañoLote)
            {
                await GuardarAsync(contexto, pendientes);
            }
        }

        await GuardarAsync(contexto, pendientes);
        await contexto.RecargarMapasAsync();

        informe.Importados(Nombre, importadas);
        if (fantasmas.Count > 0)
        {
            informe.Aviso($"Obras fantasma creadas (venta vieja de una obra que ya no está en el catálogo): {fantasmas.Count}.");
        }

        if (sinFecha > 0)
        {
            informe.Aviso($"Ventas descartadas por fecha imposible: {sinFecha}.");
        }
    }

    /// <summary>
    /// Primera pasada por la hoja: junta las obras referidas por alguna venta que no existen en el
    /// catálogo, las crea con existencia 0 y devuelve el mapa (códArtista, códObra) → Id.
    /// </summary>
    private async Task<Dictionary<(int, int), int>> CrearObrasFantasmaAsync(Fuentes fuentes, Contexto contexto)
    {
        var candidatas = new Dictionary<(int, int), Obra>();

        foreach (var fila in fuentes.Principal.Filas("Ventas", filasEncabezado: 2))
        {
            var codArtista = fila.Entero(ColCodArtista);
            var codObra = fila.Entero(ColCodObra);
            var titulo = fila.Texto(ColObra);
            if (codArtista is null || codObra is null || titulo is null)
            {
                continue;
            }

            var clave = (codArtista.Value, codObra.Value);
            if (contexto.ObraPorCodigo.ContainsKey(clave) || candidatas.ContainsKey(clave))
            {
                continue;
            }

            if (!contexto.ArtistaPorCodigo.TryGetValue(codArtista.Value, out var artistaId))
            {
                continue;
            }

            var fecha = fila.Fecha(ColFecha);
            candidatas[clave] = new Obra
            {
                ArtistaId = artistaId,
                NumeroObra = codObra.Value,
                Titulo = Recortar(titulo, 200)!,
                Moneda = LeerMoneda(fila.Texto(ColMoneda)) ?? Moneda.Pesos,
                Costo = Math.Max(0, fila.Decimal(ColCosto) ?? 0),
                Utilidad = 50,
                TieneIVA = false,
                PrecioVenta = Math.Max(0, fila.Decimal(ColPrecio) ?? 0),
                Existencia = 0,
                Estado = EstadoObra.SinStock,
                FechaIngreso = fecha is { Year: >= 2000 and <= 2030 } ? fecha.Value : new DateOnly(2009, 1, 1),
                Observaciones = "Obra reconstruida a partir de una venta histórica (no estaba en el catálogo).",
            };
        }

        if (candidatas.Count == 0)
        {
            return [];
        }

        contexto.Db.Obras.AddRange(candidatas.Values);
        await contexto.Db.SaveChangesAsync();

        var mapa = candidatas.ToDictionary(p => p.Key, p => p.Value.Id);
        contexto.Db.ChangeTracker.Clear();
        return mapa;
    }

    private static async Task GuardarAsync(Contexto contexto, List<Venta> pendientes)
    {
        if (pendientes.Count == 0)
        {
            return;
        }

        contexto.Db.Ventas.AddRange(pendientes);
        await contexto.Db.SaveChangesAsync();
        contexto.Db.ChangeTracker.Clear();
        pendientes.Clear();
    }

    private static async Task<Dictionary<int, Moneda>> CargarMonedasObraAsync(Contexto contexto)
    {
        var lista = await contexto.Db.Obras.Select(o => new { o.Id, o.Moneda }).ToListAsync();
        return lista.ToDictionary(x => x.Id, x => x.Moneda);
    }

    private static Moneda? LeerMoneda(string? texto) => Texto.Clave(texto) switch
    {
        "pesos" or "peso" or "uyu" => Moneda.Pesos,
        "dolares" or "dolar" or "usd" or "u s" or "us" => Moneda.USD,
        _ => null
    };

    private static string? Recortar(string? valor, int max) =>
        valor is not null && valor.Length > max ? valor[..max] : valor;
}
