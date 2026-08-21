namespace Galeria.Application.Devoluciones;

public interface IDevolucionRepository
{
    // Solo ventas sin devolución registrada todavía (Devolucion es 1:1 con Venta).
    Task<List<VentaParaDevolucion>> BuscarVentasAsync(string? texto, CancellationToken ct = default);

    Task AgregarAsync(Domain.Entities.Devolucion devolucion, CancellationToken ct = default);

    Task<List<DevolucionListItem>> BuscarAsync(DevolucionFiltro filtro, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
