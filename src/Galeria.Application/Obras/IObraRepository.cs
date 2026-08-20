namespace Galeria.Application.Obras;

public interface IObraRepository
{
    Task<List<ObraListItem>> BuscarAsync(string? textoLibre, int? artistaId, CancellationToken ct = default);

    // Nombre idéntico dentro del mismo artista (decisión #6): candidata a "agregar existencia"
    // en vez de crear una obra duplicada.
    Task<ObraCoincidente?> BuscarCoincidenciaAsync(int artistaId, string titulo, CancellationToken ct = default);

    Task<int> ProximoNumeroObraAsync(int artistaId, CancellationToken ct = default);

    Task AgregarAsync(Domain.Entities.Obra obra, CancellationToken ct = default);

    Task AumentarExistenciaAsync(int obraId, int cantidad, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
