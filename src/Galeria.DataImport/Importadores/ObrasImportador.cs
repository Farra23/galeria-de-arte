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

    public async Task EjecutarAsync(Fuentes fuentes, Contexto contexto, Informe informe)
    {
        await AsegurarCatalogosAsync(fuentes, contexto);

        var claves = new HashSet<(int Artista, int Obra)>();
        var importados = 0;
        var sinMoneda = 0;
        var sinFecha = 0;
        var pendientes = new List<Obra>(TamañoLote);

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

            if (!claves.Add((artistaId.Value, numObra.Value)))
            {
                informe.Rechazo(Nombre, "código de obra duplicado (mismo artista + número)");
                continue;
            }

            var moneda = LeerMoneda(fila.Texto(ColMoneda));
            if (moneda is null)
            {
                sinMoneda++;
            }

            var fecha = fila.Fecha(ColFechaIngreso);
            if (fecha is null || fecha.Value.Year is < 1990 or > 2100)
            {
                sinFecha++;
                fecha = FechaIngresoPorDefecto;
            }

            var existencia = Math.Max(0, fila.Entero(ColExistencia) ?? 0);

            pendientes.Add(new Obra
            {
                ArtistaId = artistaId.Value,
                NumeroObra = numObra.Value,
                Titulo = Recortar(titulo, 200)!,
                Moneda = moneda ?? Moneda.Pesos,
                Costo = Math.Max(0, fila.Decimal(ColCosto) ?? 0),
                Utilidad = fila.Decimal(ColUtilidad) ?? 50,
                TieneIVA = fila.Booleano(ColIva) ?? false,
                PrecioVenta = Math.Max(0, fila.Decimal(ColPrecioVenta) ?? 0),
                Existencia = existencia,
                PagoContado = fila.Booleano(ColPagoContado) ?? false,
                FechaIngreso = fecha.Value,
                Estado = existencia > 0 ? EstadoObra.Disponible : EstadoObra.SinStock,
                AltoCm = Positivo(fila.Decimal(ColAlto)),
                AnchoCm = Positivo(fila.Decimal(ColAncho)),
                LargoCm = Positivo(fila.Decimal(ColLargo)),
                Observaciones = Recortar(fila.Texto(ColObs), 2000),
                RubroId = BuscarCatalogo(_rubros, fila.Texto(ColRubro)),
                TecnicaId = BuscarCatalogo(_tecnicas, fila.Texto(ColTecnica)),
            });
            importados++;

            if (pendientes.Count >= TamañoLote)
            {
                await GuardarAsync(contexto, pendientes);
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
            informe.Aviso($"Obras sin moneda en el Excel: {sinMoneda} — se cargaron como Pesos (revisar con el cliente).");
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
