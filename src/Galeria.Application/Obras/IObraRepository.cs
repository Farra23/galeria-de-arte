namespace Galeria.Application.Obras;

public interface IObraRepository
{
    Task<List<ObraListItem>> BuscarAsync(ObraFiltro filtro, CancellationToken ct = default);

    // Nombre idéntico dentro del mismo artista (decisión #6): candidata a "agregar existencia"
    // en vez de crear una obra duplicada.
    Task<ObraCoincidente?> BuscarCoincidenciaAsync(int artistaId, string titulo, CancellationToken ct = default);

    Task<int> ProximoNumeroObraAsync(int artistaId, CancellationToken ct = default);

    Task AgregarAsync(Domain.Entities.Obra obra, CancellationToken ct = default);

    Task AumentarExistenciaAsync(int obraId, int cantidad, CancellationToken ct = default);

    Task<ObraFicha?> ObtenerFichaAsync(int id, CancellationToken ct = default);

    Task ActualizarAsync(ActualizarObraRequest request, CancellationToken ct = default);

    // Entidad rastreada (no DTO): la usan otros módulos (Ventas, Retiros, Alquileres, Devoluciones)
    // que necesitan mutar Existencia/Estado como parte de su propia operación. Obra sigue siendo
    // dueña de su propia persistencia — los demás módulos no tocan la tabla Obras directamente.
    Task<Domain.Entities.Obra?> ObtenerEntidadAsync(int id, CancellationToken ct = default);

    // El libro de movimientos de stock (ver Domain.Entities.Movimiento) también es responsabilidad
    // de este repositorio: Obra es el agregado dueño de su propio historial de stock.
    Task RegistrarMovimientoAsync(Domain.Entities.Movimiento movimiento, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
