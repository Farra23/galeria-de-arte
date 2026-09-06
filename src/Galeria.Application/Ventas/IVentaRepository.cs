namespace Galeria.Application.Ventas;

public interface IVentaRepository
{
    Task AgregarAsync(Domain.Entities.Venta venta, CancellationToken ct = default);

    // Entidad rastreada: la usa Devoluciones para leer Cantidad/ObraId y revertir la venta.
    Task<Domain.Entities.Venta?> ObtenerEntidadAsync(int id, CancellationToken ct = default);

    Task<List<VentaListItem>> BuscarAsync(VentaFiltro filtro, CancellationToken ct = default);

    // Las N más recientes por fecha, ya limitadas en la consulta -- ver comentario en el repositorio.
    Task<List<VentaListItem>> ObtenerUltimasAsync(int cantidad, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
