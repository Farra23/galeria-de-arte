namespace Galeria.Application.Retiros;

public interface IRetiroRepository
{
    Task AgregarAsync(Domain.Entities.Retiro retiro, CancellationToken ct = default);

    // Entidad rastreada: la necesita DevolverAsync para mutar FechaDevolucion.
    Task<Domain.Entities.Retiro?> ObtenerEntidadAsync(int id, CancellationToken ct = default);

    // DTO de solo lectura para el PDF del comprobante (requerimiento 6.2).
    Task<RetiroListItem?> ObtenerAsync(int id, CancellationToken ct = default);

    Task<List<RetiroListItem>> BuscarAsync(RetiroFiltro filtro, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
