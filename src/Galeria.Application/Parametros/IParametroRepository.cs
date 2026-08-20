namespace Galeria.Application.Parametros;

public interface IParametroRepository
{
    Task<string?> ObtenerAsync(string clave, CancellationToken ct = default);
}
