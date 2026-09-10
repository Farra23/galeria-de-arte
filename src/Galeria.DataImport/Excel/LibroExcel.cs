using System.Globalization;
using ClosedXML.Excel;

namespace Galeria.DataImport.Excel;

/// <summary>
/// Envoltura fina sobre ClosedXML: abre un libro y expone sus hojas como secuencias de
/// <see cref="Fila"/> con accesores tipados y tolerantes a datos sucios.
/// </summary>
public sealed class LibroExcel(string ruta) : IDisposable
{
    private readonly XLWorkbook _libro = new(ruta);

    public string Nombre { get; } = Path.GetFileName(ruta);

    public bool TieneHoja(string nombre) => _libro.TryGetWorksheet(nombre, out _);

    /// <summary>
    /// Filas de datos de una hoja, saltando las primeras <paramref name="filasEncabezado"/>.
    /// Recorre solo las filas con contenido (las planillas arrastran formato hasta el millón de
    /// filas: iterar por número de fila sería carísimo).
    /// </summary>
    public IEnumerable<Fila> Filas(string hoja, int filasEncabezado)
    {
        if (!_libro.TryGetWorksheet(hoja, out var ws))
        {
            yield break;
        }

        foreach (var fila in ws.RowsUsed())
        {
            if (fila.RowNumber() <= filasEncabezado)
            {
                continue;
            }

            var envuelta = new Fila(fila, fila.RowNumber());
            if (!envuelta.EstaVacia)
            {
                yield return envuelta;
            }
        }
    }

    public void Dispose() => _libro.Dispose();
}

/// <summary>Una fila de Excel. Columnas 1-based, igual que Excel.</summary>
public sealed class Fila(IXLRow fila, int numero)
{
    public int Numero { get; } = numero;

    public bool EstaVacia => fila.CellsUsed().All(c => Bruto(c.Address.ColumnNumber) is null);

    /// <summary>Valor crudo de la celda como string/double/DateTime/bool/null, sin evaluar fórmulas.</summary>
    private object? Bruto(int columna)
    {
        var celda = fila.Cell(columna);
        var valor = celda.HasFormula ? celda.CachedValue : celda.Value;

        return valor.Type switch
        {
            XLDataType.Blank => null,
            XLDataType.Boolean => valor.GetBoolean(),
            XLDataType.Number => valor.GetNumber(),
            XLDataType.DateTime => valor.GetDateTime(),
            XLDataType.TimeSpan => valor.GetTimeSpan(),
            XLDataType.Text => string.IsNullOrWhiteSpace(valor.GetText()) ? null : valor.GetText(),
            XLDataType.Error => null,
            _ => null
        };
    }

    public string? Texto(int columna) => Bruto(columna) switch
    {
        null => null,
        string s => Galeria.DataImport.Texto.Limpiar(s),
        double d => d.ToString(CultureInfo.InvariantCulture),
        bool b => b ? "True" : "False",
        DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        var otro => otro.ToString()
    };

    public decimal? Decimal(int columna) => Bruto(columna) switch
    {
        double d => (decimal)d,
        string s when decimal.TryParse(s.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) => v,
        string s when decimal.TryParse(s, NumberStyles.Any, new CultureInfo("es-UY"), out var v) => v,
        _ => null
    };

    public int? Entero(int columna) => Bruto(columna) switch
    {
        double d => (int)Math.Round(d),
        string s when int.TryParse(s.Trim(), out var v) => v,
        string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => (int)Math.Round(d),
        _ => null
    };

    public DateOnly? Fecha(int columna) => Bruto(columna) switch
    {
        DateTime dt => DateOnly.FromDateTime(dt),
        double d => TryFromSerial(d),
        string s => TryParseFecha(s),
        _ => null
    };

    public bool? Booleano(int columna) => Bruto(columna) switch
    {
        bool b => b,
        double d => d != 0,
        string s => s.Trim().ToLowerInvariant() switch
        {
            "true" or "verdadero" or "si" or "sí" or "x" or "1" => true,
            "false" or "falso" or "no" or "0" => false,
            _ => null
        },
        _ => null
    };

    private static DateOnly? TryFromSerial(double serial)
    {
        try
        {
            return serial is > 0 and < 80000
                ? DateOnly.FromDateTime(DateTime.FromOADate(serial))
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static DateOnly? TryParseFecha(string s)
    {
        string[] formatos = ["d/M/yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "d-M-yyyy", "M/d/yyyy", "yyyy/MM/dd"];
        if (DateOnly.TryParseExact(s.Trim(), formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var f))
        {
            return f;
        }

        return DateTime.TryParse(s, new CultureInfo("es-UY"), DateTimeStyles.None, out var dt)
            ? DateOnly.FromDateTime(dt)
            : null;
    }
}
