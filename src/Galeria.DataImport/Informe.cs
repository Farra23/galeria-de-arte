using System.Text;

namespace Galeria.DataImport;

/// <summary>
/// Acumula el resultado de la importación: cuántos registros entraron de cada tipo y, sobre todo,
/// qué filas se rechazaron y por qué. El archivo que deja es la lista de trabajo para resolver a
/// mano con el cliente (fechas imposibles, moneda faltante, artistas con el nombre partido, etc.).
/// </summary>
public sealed class Informe
{
    private readonly List<string> _secciones = [];
    private readonly Dictionary<string, int> _importados = [];
    private readonly Dictionary<string, List<string>> _rechazos = [];
    private readonly List<string> _avisos = [];

    public void Importados(string entidad, int cantidad) => _importados[entidad] = cantidad;

    public void Rechazo(string entidad, string motivo)
    {
        if (!_rechazos.TryGetValue(entidad, out var lista))
        {
            _rechazos[entidad] = lista = [];
        }

        lista.Add(motivo);
    }

    public void Aviso(string texto) => _avisos.Add(texto);

    /// <summary>
    /// Detecta nombres de catálogo casi iguales ("Ensamble" / "Ensamblajes" / "Esamblajes") para
    /// que el cliente decida si son lo mismo y los unifique desde Configuración.
    /// </summary>
    public void RevisarParecidos(string tipo, IReadOnlyList<string> nombres)
    {
        var pares = new List<string>();
        for (var i = 0; i < nombres.Count; i++)
        {
            for (var j = i + 1; j < nombres.Count; j++)
            {
                var a = Texto.QuitarAcentos(nombres[i]).ToLowerInvariant().Replace(" ", "");
                var b = Texto.QuitarAcentos(nombres[j]).ToLowerInvariant().Replace(" ", "");
                if (a.Length >= 4 && b.Length >= 4 && DistanciaLevenshtein(a, b) <= 2)
                {
                    pares.Add($"«{nombres[i]}» ≈ «{nombres[j]}»");
                }
            }
        }

        foreach (var par in pares)
        {
            _avisos.Add($"{tipo} parecidos (¿unificar?): {par}");
        }
    }

    private static int DistanciaLevenshtein(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++)
        {
            d[i, 0] = i;
        }

        for (var j = 0; j <= b.Length; j++)
        {
            d[0, j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var costo = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + costo);
            }
        }

        return d[a.Length, b.Length];
    }

    public void Seccion(string titulo) => _secciones.Add(titulo);

    public int TotalRechazos => _rechazos.Values.Sum(l => l.Count);

    public void Escribir(string ruta)
    {
        var sb = new StringBuilder();
        sb.AppendLine("INFORME DE IMPORTACIÓN — ERP Galería ACATRAS");
        sb.AppendLine($"Generado: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine(new string('=', 78));

        sb.AppendLine();
        sb.AppendLine("REGISTROS IMPORTADOS");
        sb.AppendLine(new string('-', 78));
        foreach (var (entidad, cantidad) in _importados)
        {
            sb.AppendLine($"  {entidad,-30} {cantidad,8:N0}");
        }

        if (_avisos.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("AVISOS");
            sb.AppendLine(new string('-', 78));
            foreach (var aviso in _avisos)
            {
                sb.AppendLine($"  - {aviso}");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"FILAS NO IMPORTADAS  (total: {TotalRechazos:N0})");
        sb.AppendLine(new string('-', 78));
        if (_rechazos.Count == 0)
        {
            sb.AppendLine("  (ninguna)");
        }

        foreach (var (entidad, motivos) in _rechazos)
        {
            sb.AppendLine();
            sb.AppendLine($"  [{entidad}]  {motivos.Count:N0} filas");
            var agrupados = motivos
                .GroupBy(m => m)
                .OrderByDescending(g => g.Count());
            foreach (var grupo in agrupados)
            {
                sb.AppendLine($"     {grupo.Count(),6:N0} x  {grupo.Key}");
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ruta))!);
        File.WriteAllText(ruta, sb.ToString());
    }

    public void ImprimirResumen()
    {
        Console.WriteLine();
        Console.WriteLine("Resumen:");
        foreach (var (entidad, cantidad) in _importados)
        {
            Console.WriteLine($"  {entidad,-30} {cantidad,8:N0}");
        }

        Console.WriteLine($"  {"filas rechazadas",-30} {TotalRechazos,8:N0}");
    }
}
