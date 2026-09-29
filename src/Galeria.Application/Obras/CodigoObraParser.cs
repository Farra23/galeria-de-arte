namespace Galeria.Application.Obras;

// Punto 18 del testeo de cliente: poder encontrar una obra por su código físico (impreso en la
// etiqueta) sin tener que escribir el título exacto. El código visible es CódigoArtista +
// NúmeroObra pegados sin separador, con NúmeroObra siempre en los últimos 3 dígitos (mismo
// formato que ObraListItem.CodigoVisible) — por eso separar por la derecha, no por posición fija,
// también funciona si el código de artista ya pasó de 3 dígitos (ver punto 21 del testeo).
public static class CodigoObraParser
{
    public static (int CodigoArtista, int NumeroObra)? Parsear(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var soloDigitos = new string(texto.Where(char.IsDigit).ToArray());

        // Con 3 dígitos o menos no hay forma de separar "parte del artista" de "parte de la
        // obra" sin ambigüedad — se lo trata como no reconocido en vez de adivinar.
        if (soloDigitos.Length <= 3)
        {
            return null;
        }

        var parteArtista = soloDigitos[..^3];
        var parteObra = soloDigitos[^3..];

        if (!int.TryParse(parteArtista, out var codigoArtista) || !int.TryParse(parteObra, out var numeroObra))
        {
            return null;
        }

        return (codigoArtista, numeroObra);
    }
}
