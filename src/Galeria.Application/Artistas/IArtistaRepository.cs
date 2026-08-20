namespace Galeria.Application.Artistas;

// Application define el contrato, Infrastructure lo implementa (Dependency Inversion):
// el servicio de más abajo depende de esta interfaz, nunca de EF Core directamente.
public interface IArtistaRepository
{
    Task<List<ArtistaListItem>> BuscarAsync(string? textoLibre, CancellationToken ct = default);

    Task<List<ArtistaOpcion>> ListarActivosAsync(CancellationToken ct = default);

    Task<int> ProximoCodigoAsync(CancellationToken ct = default);

    Task AgregarAsync(Domain.Entities.Artista artista, CancellationToken ct = default);

    Task<ArtistaFicha?> ObtenerFichaAsync(int id, CancellationToken ct = default);

    Task ActualizarAsync(ActualizarArtistaRequest request, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
