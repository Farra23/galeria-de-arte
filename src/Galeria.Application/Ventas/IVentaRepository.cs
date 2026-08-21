namespace Galeria.Application.Ventas;

public interface IVentaRepository
{
    // Solo ofrece obras Disponible con existencia > 0 (una obra retirada/alquilada/sin stock
    // no puede venderse — requerimiento 0.5).
    Task<List<ObraParaVenta>> BuscarObrasDisponiblesAsync(string? texto, CancellationToken ct = default);

    Task<ObraParaVenta?> ObtenerObraParaVentaAsync(int obraId, CancellationToken ct = default);

    Task AgregarAsync(Domain.Entities.Venta venta, CancellationToken ct = default);

    Task<List<VentaListItem>> BuscarAsync(VentaFiltro filtro, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
