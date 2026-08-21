namespace Galeria.Application.Alquileres;

public interface IAlquilerRepository
{
    Task AgregarAsync(Domain.Entities.Alquiler alquiler, CancellationToken ct = default);

    Task<Domain.Entities.Alquiler?> ObtenerEntidadAsync(int id, CancellationToken ct = default);

    Task<List<AlquilerListItem>> BuscarAsync(AlquilerFiltro filtro, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
