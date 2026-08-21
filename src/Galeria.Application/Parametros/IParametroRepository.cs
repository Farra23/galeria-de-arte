namespace Galeria.Application.Parametros;

public interface IParametroRepository
{
    Task<string?> ObtenerAsync(string clave, CancellationToken ct = default);

    Task<Dictionary<string, string>> ObtenerVariosAsync(IEnumerable<string> claves, CancellationToken ct = default);

    Task GuardarAsync(string clave, string? valor, CancellationToken ct = default);
}
