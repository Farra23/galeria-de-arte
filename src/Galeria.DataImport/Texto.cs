using System.Globalization;
using System.Text;

namespace Galeria.DataImport;

/// <summary>
/// Normalización de texto para cotejar datos sucios de las planillas: nombres de artista escritos
/// de varias formas, técnicas con mayúsculas y tildes inconsistentes, etc.
/// </summary>
public static class Texto
{
    /// <summary>Recorta, colapsa espacios internos y convierte "" en null.</summary>
    public static string? Limpiar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var partes = valor.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', partes);
    }

    /// <summary>
    /// Clave de comparación: sin tildes, sin mayúsculas, sin espacios de más, sin signos de
    /// puntuación al borde. "  García , Pedro " y "GARCIA,PEDRO" dan la misma clave.
    /// </summary>
    public static string Clave(string? valor)
    {
        var limpio = Limpiar(valor) ?? string.Empty;
        var sinAcentos = QuitarAcentos(limpio).ToLowerInvariant();

        var sb = new StringBuilder(sinAcentos.Length);
        foreach (var c in sinAcentos)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
            else if (c is ' ' or ',')
            {
                sb.Append(' ');
            }
        }

        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public static string QuitarAcentos(string valor)
    {
        var descompuesto = valor.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Convierte "Título en MAYÚSCULAS" o "título en minúsculas" a "Título Normal" respetando
    /// palabras cortas (de, y, con, s/). Se usa para técnicas y rubros del Excel viejo.
    /// </summary>
    public static string TituloProlijo(string valor)
    {
        var limpio = Limpiar(valor) ?? string.Empty;
        var palabras = limpio.ToLower(new CultureInfo("es-UY")).Split(' ');
        var menores = new HashSet<string> { "de", "del", "la", "el", "y", "con", "sin", "en", "a", "s/", "c/", "para" };

        for (var i = 0; i < palabras.Length; i++)
        {
            var p = palabras[i];
            if (p.Length == 0)
            {
                continue;
            }

            if (i > 0 && menores.Contains(p))
            {
                continue;
            }

            palabras[i] = char.ToUpper(p[0], new CultureInfo("es-UY")) + p[1..];
        }

        return string.Join(' ', palabras);
    }
}
