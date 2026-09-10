using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Galeria.DataImport.Importadores;

/// <summary>
/// Hoja "Artistas" de la planilla principal. El maestro tiene ~265 filas, varias son ranuras
/// vacías (solo un código) y hay que descartarlas. El código de artista se respeta tal cual
/// (decisión: hay huecos históricos del 102 al 990, no es un autonumérico).
///
/// Además corrige nombres mal cargados en el Excel: casos donde el apellido repite el nombre
/// ("Giuliana Perotti" / "Giuliana") o donde un nombre de taller quedó partido en dos columnas.
/// </summary>
public sealed class ArtistasImportador : IImportador
{
    public string Nombre => "Artistas";

    private const int ColCodigo = 1, ColApellido = 2, ColNombre = 3, ColPerfil = 4,
        ColTaller = 5, ColCelular = 6, ColTelFijo = 7, ColDireccion = 9, ColCorreo = 10;

    // Correcciones puntuales que la regla general no puede resolver sola (nombre de taller
    // partido, o apellido de 3+ palabras). Formato: código → (Apellido, Nombre) correctos.
    private static readonly Dictionary<int, (string Apellido, string Nombre)> Correcciones = new()
    {
        [564] = ("Ni gatos ni limones", ""),   // estaba partido: "gatos ni limones" / "Ni"
        [997] = ("Pastorino", "Sandra"),        // estaba: "Pastorino Sandra" / ""
        [998] = ("Esposito", "Lorena"),         // estaba: "Lorena Esposito" / ""
        [958] = ("Umpierrez", "Silvia"),        // estaba al revés: "Silvia" / "Umpierrez" (canónico de la fusión con 670)
    };

    public async Task EjecutarAsync(Fuentes fuentes, Contexto contexto, Informe informe)
    {
        var codigosVistos = new HashSet<int>();
        var importados = 0;
        var corregidos = 0;

        foreach (var fila in fuentes.Principal.Filas("Artistas", filasEncabezado: 1))
        {
            var codigo = fila.Entero(ColCodigo);
            var apellido = Recortar(fila.Texto(ColApellido), 100);
            var nombre = Recortar(fila.Texto(ColNombre), 100);

            if (codigo is null)
            {
                informe.Rechazo(Nombre, "fila sin código de artista");
                continue;
            }

            if (apellido is null && nombre is null)
            {
                informe.Rechazo(Nombre, "ranura de código sin nombre (fila vacía del maestro)");
                continue;
            }

            if (!codigosVistos.Add(codigo.Value))
            {
                informe.Rechazo(Nombre, $"código de artista duplicado ({codigo})");
                continue;
            }

            var (apFinal, noFinal, seCorrigio) = LimpiarNombre(codigo.Value, apellido ?? nombre!, nombre ?? string.Empty);
            if (seCorrigio)
            {
                corregidos++;
            }

            contexto.Db.Artistas.Add(new Artista
            {
                Codigo = codigo.Value,
                Apellido = apFinal,
                Nombre = noFinal,
                Perfil = Recortar(fila.Texto(ColPerfil), 2000),
                Taller = Recortar(fila.Texto(ColTaller), 200),
                Celular = Recortar(fila.Texto(ColCelular), 30),
                TelFijo = Recortar(fila.Texto(ColTelFijo), 30),
                Direccion = Recortar(fila.Texto(ColDireccion), 300),
                Correo = Recortar(fila.Texto(ColCorreo), 200),
                Activo = true,
            });
            importados++;
        }

        await contexto.Db.SaveChangesAsync();
        await contexto.RecargarMapasAsync();
        informe.Importados(Nombre, importados);
        if (corregidos > 0)
        {
            informe.Aviso($"Nombres de artista corregidos (apellido repetía el nombre, o taller partido): {corregidos}.");
        }

        await DesactivarAsync(fuentes, contexto, informe);
        ReportarPosiblesDuplicados(contexto, informe);
    }

    /// <summary>
    /// Marca como inactivos los artistas listados en <c>datos-origen/artistas-inactivos.csv</c>
    /// (un código por línea). Un artista inactivo deja de aparecer en los desplegables y en la
    /// lista de Artistas, pero conserva toda su historia. Se usa para el duplicado que el cliente
    /// confirma que no va más (el "bueno" queda activo).
    /// </summary>
    private static async Task DesactivarAsync(Fuentes fuentes, Contexto contexto, Informe informe)
    {
        var ruta = Path.Combine(fuentes.CarpetaOrigen, "artistas-inactivos.csv");
        if (!File.Exists(ruta))
        {
            return;
        }

        var desactivados = 0;
        foreach (var linea in File.ReadAllLines(ruta))
        {
            var texto = linea.Split('#')[0].Trim();
            if (texto.Length == 0 || !int.TryParse(texto, out var codigo))
            {
                continue;
            }

            var artista = await contexto.Db.Artistas.FirstOrDefaultAsync(a => a.Codigo == codigo);
            if (artista is null)
            {
                informe.Rechazo("Artistas inactivos", $"código {codigo} no existe");
                continue;
            }

            artista.Activo = false;
            desactivados++;
        }

        if (desactivados > 0)
        {
            await contexto.Db.SaveChangesAsync();
            informe.Aviso($"Artistas marcados como inactivos (no aparecen en los desplegables): {desactivados}.");
        }
    }

    /// <summary>
    /// Marca artistas que podrían ser la misma persona: mismo nombre (aunque esté al revés), o
    /// mismo correo. No los une automáticamente — unir artistas con obras y ventas cambia saldos
    /// y códigos, es una decisión del cliente.
    /// </summary>
    private static void ReportarPosiblesDuplicados(Contexto contexto, Informe informe)
    {
        var artistas = contexto.Db.Artistas
            .Select(a => new { a.Codigo, a.Apellido, a.Nombre, a.Correo })
            .ToList();

        foreach (var grupo in artistas
                     .GroupBy(a => Texto.Clave(string.Join(' ', new[] { a.Apellido, a.Nombre }.OrderBy(x => x))))
                     .Where(g => g.Count() > 1))
        {
            var lista = string.Join(" / ", grupo.Select(a => $"cod {a.Codigo} «{a.Apellido}, {a.Nombre}»"));
            informe.Aviso($"Artistas con el mismo nombre (¿son la misma persona?): {lista}");
        }

        foreach (var grupo in artistas
                     .Where(a => a.Correo is not null && a.Correo.Contains('@'))
                     .GroupBy(a => Texto.Clave(a.Correo))
                     .Where(g => g.Select(x => Texto.Clave($"{x.Apellido} {x.Nombre}")).Distinct().Count() > 1))
        {
            var lista = string.Join(" / ", grupo.Select(a => $"cod {a.Codigo} «{a.Apellido}, {a.Nombre}»"));
            informe.Aviso($"Artistas con el mismo correo pero distinto nombre (¿familia con mail compartido, o duplicado?): {lista}");
        }
    }

    /// <summary>
    /// Devuelve el (Apellido, Nombre) corregido. Reglas:
    ///  1. Corrección puntual por código (<see cref="Correcciones"/>).
    ///  2. Si el nombre no está vacío y el apellido son exactamente dos palabras, una de las
    ///     cuales es el nombre → el apellido queda con la otra palabra
    ///     ("Giuliana Perotti" + "Giuliana" → "Perotti" + "Giuliana").
    /// </summary>
    private static (string Apellido, string Nombre, bool Corregido) LimpiarNombre(int codigo, string apellido, string nombre)
    {
        if (Correcciones.TryGetValue(codigo, out var fijo))
        {
            return (fijo.Apellido, fijo.Nombre, true);
        }

        var palabras = apellido.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (nombre.Length > 0 && palabras.Length == 2)
        {
            var claveNombre = Texto.Clave(nombre);
            var otras = palabras.Where(p => Texto.Clave(p) != claveNombre).ToArray();
            if (otras.Length == 1)
            {
                return (otras[0], nombre, true);
            }
        }

        return (apellido, nombre, false);
    }

    private static string? Recortar(string? valor, int max)
    {
        var limpio = Texto.Limpiar(valor);
        return limpio is not null && limpio.Length > max ? limpio[..max] : limpio;
    }
}
