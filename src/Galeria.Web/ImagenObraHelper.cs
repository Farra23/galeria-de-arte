using Microsoft.AspNetCore.Components.Forms;

namespace Galeria.Web;

// Requerimiento 3.2: subir imagen de la obra, al cargarla o más tarde. Un solo archivo por obra
// (la variante "varias fotos con una principal" del requerimiento queda para más adelante, es un
// [+] propio no confirmado con el cliente). Se guarda en wwwroot/uploads/obras con el Id de la
// obra como nombre — así reemplazar la imagen es simplemente sobrescribir, sin acumular huérfanos.
public static class ImagenObraHelper
{
    private const long TamanioMaximoBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] ExtensionesPermitidas = [".jpg", ".jpeg", ".png", ".webp"];

    public static bool EsValida(IBrowserFile archivo, out string? error)
    {
        var extension = Path.GetExtension(archivo.Name).ToLowerInvariant();

        if (!ExtensionesPermitidas.Contains(extension))
        {
            error = "Formato no admitido — usá JPG, PNG o WEBP.";
            return false;
        }

        if (archivo.Size > TamanioMaximoBytes)
        {
            error = "La imagen no puede pesar más de 5 MB.";
            return false;
        }

        error = null;
        return true;
    }

    // Devuelve la ruta a servir (ej. "/uploads/obras/42.jpg") para guardar en la obra.
    public static async Task<string> GuardarAsync(IBrowserFile archivo, int obraId, string wwwrootPath, CancellationToken ct = default)
    {
        var carpeta = Path.Combine(wwwrootPath, "uploads", "obras");
        Directory.CreateDirectory(carpeta);

        // Si ya había una imagen con otra extensión, se borra para no dejar huérfanos.
        foreach (var existente in Directory.EnumerateFiles(carpeta, $"{obraId}.*"))
        {
            File.Delete(existente);
        }

        var extension = Path.GetExtension(archivo.Name).ToLowerInvariant();
        var nombreArchivo = $"{obraId}{extension}";
        var rutaFisica = Path.Combine(carpeta, nombreArchivo);

        await using var destino = File.Create(rutaFisica);
        await using var origen = archivo.OpenReadStream(TamanioMaximoBytes, ct);
        await origen.CopyToAsync(destino, ct);

        return $"/uploads/obras/{nombreArchivo}";
    }

    public static void Eliminar(int obraId, string wwwrootPath)
    {
        var carpeta = Path.Combine(wwwrootPath, "uploads", "obras");
        if (!Directory.Exists(carpeta))
        {
            return;
        }

        foreach (var existente in Directory.EnumerateFiles(carpeta, $"{obraId}.*"))
        {
            File.Delete(existente);
        }
    }
}
