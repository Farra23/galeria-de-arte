namespace Galeria.Application.Obras;

public interface IObraRepository
{
    Task<List<ObraListItem>> BuscarAsync(ObraFiltro filtro, CancellationToken ct = default);

    // Nombre idéntico dentro del mismo artista (decisión #6): candidata a "agregar existencia"
    // en vez de crear una obra duplicada.
    Task<ObraCoincidente?> BuscarCoincidenciaAsync(int artistaId, string titulo, CancellationToken ct = default);

    Task<int> ProximoNumeroObraAsync(int artistaId, CancellationToken ct = default);

    Task AgregarAsync(Domain.Entities.Obra obra, CancellationToken ct = default);

    // Serie (requerimiento 3.3): un conjunto de piezas que comparten formulario de alta pero no
    // el código. Obra sigue siendo dueña de la persistencia — Serie no tiene entidad propia en
    // el mundo de la aplicación, solo existe como agrupador de Obras.
    Task AgregarSerieAsync(Domain.Entities.Serie serie, CancellationToken ct = default);

    // Solo series con al menos una obra (para el combo de filtro de la Lista — requerimiento 3.3).
    Task<List<SerieOpcion>> ListarSeriesAsync(CancellationToken ct = default);

    Task AumentarExistenciaAsync(int obraId, int cantidad, CancellationToken ct = default);

    Task<ObraFicha?> ObtenerFichaAsync(int id, CancellationToken ct = default);

    Task ActualizarAsync(ActualizarObraRequest request, CancellationToken ct = default);

    // Cambio de precio unitario, sin tocar el resto de la ficha (requerimiento 12: cambio de
    // precios global toca muchas obras a la vez, no tiene sentido reenviar título/rubro/etc. de
    // cada una).
    Task ActualizarPrecioAsync(int obraId, decimal nuevoPrecio, CancellationToken ct = default);

    // Ruta relativa dentro de wwwroot (ej. "uploads/obras/42.jpg") o null para borrar la imagen.
    // Requerimiento 3.2: "se puede subir al cargar la obra o más tarde" — un solo método sirve
    // para los dos casos, la diferencia es solo cuándo se lo llama.
    Task ActualizarImagenAsync(int obraId, string? rutaRelativa, CancellationToken ct = default);

    // Etiquetas adhesivas con código de barras: solo importa la última vez que se imprimió, para
    // poder armar una cola de "obras pendientes de etiqueta" y permitir reimprimir en cualquier
    // momento (misma idea que ActualizarImagenAsync, un campo suelto que no pasa por el EditForm).
    Task MarcarEtiquetaImpresaAsync(int obraId, CancellationToken ct = default);

    // Entidad rastreada (no DTO): la usan otros módulos (Ventas, Retiros, Alquileres, Devoluciones)
    // que necesitan mutar Existencia/Estado como parte de su propia operación. Obra sigue siendo
    // dueña de su propia persistencia — los demás módulos no tocan la tabla Obras directamente.
    Task<Domain.Entities.Obra?> ObtenerEntidadAsync(int id, CancellationToken ct = default);

    // Búsqueda acotada a obras Disponible con existencia > 0 (una obra retirada/alquilada/sin
    // stock no puede volver a operarse — requerimiento 0.5). La reutilizan Ventas, Retiros y
    // Alquileres.
    Task<List<ObraParaOperacion>> BuscarDisponiblesAsync(string? texto, CancellationToken ct = default);

    Task<ObraParaOperacion?> ObtenerParaOperacionAsync(int id, CancellationToken ct = default);

    Task<List<MovimientoItem>> ObtenerMovimientosAsync(int obraId, CancellationToken ct = default);

    // El libro de movimientos de stock (ver Domain.Entities.Movimiento) también es responsabilidad
    // de este repositorio: Obra es el agregado dueño de su propio historial de stock.
    Task RegistrarMovimientoAsync(Domain.Entities.Movimiento movimiento, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
