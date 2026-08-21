namespace Galeria.Application.Ventas;

public interface IVentaRepository
{
    Task AgregarAsync(Domain.Entities.Venta venta, CancellationToken ct = default);

    Task<List<VentaListItem>> BuscarAsync(VentaFiltro filtro, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
