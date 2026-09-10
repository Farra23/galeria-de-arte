using Galeria.DataImport.Excel;
using Galeria.Domain.Entities;
using Galeria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Galeria.DataImport.Importadores;

/// <summary>
/// Hoja "Obras" (~14.900 filas): el catálogo completo, incluyendo piezas sin stock (histórico).
/// Es lo que más le importa al cliente que quede bien. El código de obra es el correlativo por
/// artista (columnas 2 y 3); la columna 1 es el código de 6 dígitos ya armado, que se usa solo
/// como respaldo si las otras dos vienen vacías.
/// </summary>
public sealed class ObrasImportador : IImportador
{
    public string Nombre => "Obras";

    private const int ColCodigo6 = 1, ColCodArtista = 2, ColNumObra = 3, ColArtista = 4,
        ColTitulo = 5, ColIva = 6, ColUtilidad = 7, ColMoneda = 8, ColCosto = 9,
        ColPrecioVenta = 10, ColRubro = 11, ColTecnica = 12, ColExistencia = 13,
        ColLargo = 14, ColAlto = 15, ColAncho = 16, ColObs = 17, ColPagoContado = 18, ColFechaIngreso = 19;

    private const int TamañoLote = 2000;
    private static readonly DateOnly FechaIngresoPorDefecto = new(2009, 1, 1);

    private static readonly HashSet<string> Encabezados =
        ["rubro", "rubros", "tecnica", "tecnicas", "perfil", "column1", "column2"];

    private readonly Dictionary<string, int> _rubros = [];
    private readonly Dictionary<string, int> _tecnicas = [];

    private sealed record FilaObra(
        int ArtistaId, int CodArtista, int NumObra, string Titulo, Moneda? Moneda, decimal Costo,
        decimal Utilidad, bool Iva, decimal PrecioVenta, int Existencia, bool PagoContado,
        DateOnly Fecha, decimal? Alto, decimal? Ancho, decimal? Largo, string? Obs, int? RubroId, int? TecnicaId);

    public async Task EjecutarAsync(Fuentes fuentes, Contexto contexto, Informe informe)
    {
        await AsegurarCatalogosAsync(fuentes, contexto);

        // ── Pasada 1: leer y parsear todas las filas válidas ──────────────────────────────────
        var filas = new List<FilaObra>(15000);
        var monedaPorArtista = new Dictionary<int, Dictionary<Moneda, int>>();
        var sinFecha = 0;

        foreach (var fila in fuentes.Principal.Filas("Obras", filasEncabezado: 2))
        {
            var (codArtista, numObra) = ResolverCodigos(fila);
            var titulo = fila.Texto(ColTitulo);
            if (titulo is null)
            {
                informe.Rechazo(Nombre, "obra sin nombre");
                continue;
            }

            var artistaId = contexto.ResolverArtista(codArtista, fila.Texto(ColArtista));
            if (artistaId is null)
            {
                informe.Rechazo(Nombre, "artista no encontrado para la obra");
                continue;
            }

            if (numObra is null)
            {
                informe.Rechazo(Nombre, "no se pudo determinar el número de obra");
                continue;
            }

            var moneda = LeerMoneda(fila.Texto(ColMoneda));
            if (moneda is not null)
            {
                var conteo = monedaPorArtista.TryGetValue(codArtista ?? 0, out var d) ? d : monedaPorArtista[codArtista ?? 0] = [];
                conteo[moneda.Value] = conteo.GetValueOrDefault(moneda.Value) + 1;
            }

            var fecha = fila.Fecha(ColFechaIngreso);
            if (fecha is null || fecha.Value.Year is < 1990 or > 2100)
            {
                sinFecha++;
                fecha = FechaIngresoPorDefecto;
            }

            filas.Add(new FilaObra(
                artistaId.Value, codArtista ?? 0, numObra.Value, Recortar(titulo, 200)!, moneda,
                Math.Max(0, fila.Decimal(ColCosto) ?? 0), fila.Decimal(ColUtilidad) ?? 50,
                fila.Booleano(ColIva) ?? false, Math.Max(0, fila.Decimal(ColPrecioVenta) ?? 0),
                Math.Max(0, fila.Entero(ColExistencia) ?? 0), fila.Booleano(ColPagoContado) ?? false,
                fecha.Value, Positivo(fila.Decimal(ColAlto)), Positivo(fila.Decimal(ColAncho)),
                Positivo(fila.Decimal(ColLargo)), Recortar(fila.Texto(ColObs), 2000),
                BuscarCatalogo(_rubros, fila.Texto(ColRubro)), BuscarCatalogo(_tecnicas, fila.Texto(ColTecnica))));
        }

        // Moneda dominante de cada artista (para las obras que vienen sin moneda).
        var monedaDominante = monedaPorArtista.ToDictionary(
            p => p.Key, p => p.Value.OrderByDescending(x => x.Value).First().Key);

        // Próximo número de obra libre por artista (para recodificar colisiones con stock).
        var proximoNumero = filas.GroupBy(f => f.ArtistaId)
            .ToDictionary(g => g.Key, g => g.Max(f => f.NumObra) + 1);

        // ── Pasada 2: resolver colisiones de código y persistir ───────────────────────────────
        var importados = 0;
        var sinMoneda = 0;
        var recodificadas = 0;
        var descartadasSinStock = 0;
        var pendientes = new List<Obra>(TamañoLote);

        foreach (var grupo in filas.GroupBy(f => (f.ArtistaId, f.NumObra)))
        {
            var ordenadas = grupo.OrderByDescending(f => f.Existencia).ThenByDescending(f => f.PrecioVenta).ToList();
            for (var i = 0; i < ordenadas.Count; i++)
            {
                var f = ordenadas[i];
                var numero = f.NumObra;
                string? notaColision = null;

                if (i > 0)
                {
                    // Colisión: la primera (más stock) se queda con el código; el resto…
                    if (f.Existencia == 0)
                    {
                        descartadasSinStock++;
                        informe.Rechazo(Nombre,
                            $"código {f.CodArtista:D3}{f.NumObra:D3} repetido y sin stock — se quedó «{ordenadas[0].Titulo}», se descartó «{f.Titulo}»");
                        continue;
                    }

                    numero = proximoNumero[f.ArtistaId]++;
                    recodificadas++;
                    notaColision = $"Código reasignado por colisión en la migración (era {f.CodArtista:D3}{f.NumObra:D3}).";
                    informe.Rechazo(Nombre,
                        $"código {f.CodArtista:D3}{f.NumObra:D3} repetido, la otra tenía stock — «{f.Titulo}» se recodificó a {f.CodArtista:D3}{numero:D3}");
                }

                var moneda = f.Moneda ?? monedaDominante.GetValueOrDefault(f.CodArtista, Moneda.Pesos);
                if (f.Moneda is null)
                {
                    sinMoneda++;
                }

                pendientes.Add(new Obra
                {
                    ArtistaId = f.ArtistaId,
                    NumeroObra = numero,
                    Titulo = f.Titulo,
                    Moneda = moneda,
                    Costo = f.Costo,
                    Utilidad = f.Utilidad,
                    TieneIVA = f.Iva,
                    PrecioVenta = f.PrecioVenta,
                    Existencia = f.Existencia,
                    PagoContado = f.PagoContado,
                    FechaIngreso = f.Fecha,
                    Estado = f.Existencia > 0 ? EstadoObra.Disponible : EstadoObra.SinStock,
                    AltoCm = f.Alto,
                    AnchoCm = f.Ancho,
                    LargoCm = f.Largo,
                    Observaciones = notaColision is null ? f.Obs : Recortar($"{notaColision} {f.Obs}".Trim(), 2000),
                    RubroId = f.RubroId,
                    TecnicaId = f.TecnicaId,
                });
                importados++;

                if (pendientes.Count >= TamañoLote)
                {
                    await GuardarAsync(contexto, pendientes);
                }
            }
        }

        await GuardarAsync(contexto, pendientes);
        await contexto.RecargarMapasAsync();

        var rubros = await contexto.Db.Rubros.Select(r => r.Nombre).OrderBy(n => n).ToListAsync();
        var tecnicas = await contexto.Db.Tecnicas.Select(t => t.Nombre).OrderBy(n => n).ToListAsync();
        informe.RevisarParecidos("Rubros", rubros);
        informe.RevisarParecidos("Técnicas", tecnicas);
        informe.Aviso($"Rubros: {rubros.Count} · Técnicas: {tecnicas.Count}. El Excel trae muchas " +
                      "variantes de lo mismo — conviene una pasada de limpieza desde Configuración → Rubros y técnicas.");

        informe.Importados(Nombre, importados);
        if (sinMoneda > 0)
        {
            informe.Aviso($"Obras sin moneda en el Excel: {sinMoneda} — se les puso la moneda que más usa ese artista " +
                          "en sus otras obras (o Pesos si no había otra). El cliente puede corregir las que no correspondan.");
        }

        if (recodificadas > 0)
        {
            informe.Aviso($"Obras con código repetido pero con stock: {recodificadas} — se les dio un código nuevo del " +
                          "mismo artista para no perder el stock (ver detalle en 'filas no importadas'). Hay que reimprimir esas etiquetas.");
        }

        if (descartadasSinStock > 0)
        {
            informe.Aviso($"Obras con código repetido y sin stock descartadas: {descartadasSinStock} — son históricas, no afectan el inventario.");
        }

        if (sinFecha > 0)
        {
            informe.Aviso($"Obras sin fecha de ingreso válida: {sinFecha} — se usó {FechaIngresoPorDefecto:dd/MM/yyyy}.");
        }
    }

    private static async Task GuardarAsync(Contexto contexto, List<Obra> pendientes)
    {
        if (pendientes.Count == 0)
        {
            return;
        }

        contexto.Db.Obras.AddRange(pendientes);
        await contexto.Db.SaveChangesAsync();
        contexto.Db.ChangeTracker.Clear();
        pendientes.Clear();
    }

    private (int? CodArtista, int? NumObra) ResolverCodigos(Fila fila)
    {
        var codArtista = fila.Entero(ColCodArtista);
        var numObra = fila.Entero(ColNumObra);
        if (codArtista is not null && numObra is not null)
        {
            return (codArtista, numObra);
        }

        var codigo6 = fila.Texto(ColCodigo6);
        if (codigo6 is { Length: >= 4 } && codigo6.All(char.IsDigit))
        {
            return (codArtista ?? int.Parse(codigo6[..3]), numObra ?? int.Parse(codigo6[3..]));
        }

        return (codArtista, numObra);
    }

    private static Moneda? LeerMoneda(string? texto) => Texto.Clave(texto) switch
    {
        "pesos" or "peso" or "uyu" => Moneda.Pesos,
        "dolares" or "dolar" or "usd" or "u s" or "us" => Moneda.USD,
        _ => null
    };

    private static decimal? Positivo(decimal? valor) => valor is > 0 ? valor : null;

    private static string? Recortar(string? valor, int max) =>
        valor is not null && valor.Length > max ? valor[..max] : valor;

    private static int? BuscarCatalogo(Dictionary<string, int> catalogo, string? nombre)
    {
        var clave = Texto.Clave(nombre);
        return clave.Length > 0 && catalogo.TryGetValue(clave, out var id) ? id : null;
    }

    /// <summary>
    /// Antes de importar obras: se recorre la hoja una vez para juntar todos los rubros y técnicas
    /// que aparecen (muchos no están en las hojas Rubro/Técnica) y darlos de alta de una. Así el
    /// bucle principal no toca la base para resolver el catálogo obra por obra.
    /// </summary>
    private async Task AsegurarCatalogosAsync(Fuentes fuentes, Contexto contexto)
    {
        foreach (var r in await contexto.Db.Rubros.Select(x => new { x.Id, x.Nombre }).ToListAsync())
        {
            _rubros[Texto.Clave(r.Nombre)] = r.Id;
        }

        foreach (var t in await contexto.Db.Tecnicas.Select(x => new { x.Id, x.Nombre }).ToListAsync())
        {
            _tecnicas[Texto.Clave(t.Nombre)] = t.Id;
        }

        var rubrosNuevos = new Dictionary<string, string>();
        var tecnicasNuevas = new Dictionary<string, string>();

        foreach (var fila in fuentes.Principal.Filas("Obras", filasEncabezado: 2))
        {
            Acumular(_rubros, rubrosNuevos, fila.Texto(ColRubro), 100);
            Acumular(_tecnicas, tecnicasNuevas, fila.Texto(ColTecnica), 100);
        }

        foreach (var (clave, nombre) in rubrosNuevos)
        {
            var rubro = new Rubro { Nombre = nombre };
            contexto.Db.Rubros.Add(rubro);
            await contexto.Db.SaveChangesAsync();
            _rubros[clave] = rubro.Id;
        }

        foreach (var (clave, nombre) in tecnicasNuevas)
        {
            var tecnica = new Tecnica { Nombre = nombre };
            contexto.Db.Tecnicas.Add(tecnica);
            await contexto.Db.SaveChangesAsync();
            _tecnicas[clave] = tecnica.Id;
        }

        contexto.Db.ChangeTracker.Clear();

        static void Acumular(Dictionary<string, int> existentes, Dictionary<string, string> nuevos, string? valor, int max)
        {
            var clave = Texto.Clave(valor);
            if (clave.Length == 0 || Encabezados.Contains(clave)
                || existentes.ContainsKey(clave) || nuevos.ContainsKey(clave))
            {
                return;
            }

            var prolijo = Texto.TituloProlijo(valor!);
            nuevos[clave] = prolijo.Length > max ? prolijo[..max] : prolijo;
        }
    }
}
