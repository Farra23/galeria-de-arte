using Galeria.Domain.Enums;

namespace Galeria.Application.Obras;

// Compartido por Ventas, Retiros y Alquileres: las tres operan sobre "una obra disponible para
// operar" con la misma forma de datos — reutilizar acá evita triplicar el mismo repositorio.
public record ObraParaOperacion(
    int Id,
    string CodigoVisible,
    string Titulo,
    int ArtistaId,
    string ArtistaNombre,
    int Existencia,
    Moneda Moneda,
    decimal Costo,
    decimal PrecioVenta,
    bool TieneIVA,
    string? ImagenUrl);

public record ObraListItem(
    int Id,
    string CodigoVisible,
    string Titulo,
    string ArtistaNombre,
    string? Rubro,
    string? Tecnica,
    Moneda Moneda,
    decimal Costo,
    decimal PrecioVenta,
    int Existencia,
    EstadoObra Estado,
    DateOnly FechaIngreso,
    bool PagoContado,
    int? SerieId,
    string? SerieNombre,
    string? ImagenUrl,
    DateOnly? FechaEtiquetaImpresa);

// Para el combo de filtro por Serie en la Lista de Obras (requerimiento 3.3, "agrupar/filtrar por
// serie") — solo series que tienen al menos una obra tienen sentido para filtrar.
public record SerieOpcion(int Id, string Nombre, string ArtistaNombre);

public enum OrdenObra
{
    FechaIngreso,
    Codigo,
    Titulo,
    Artista,
    Rubro,
    Tecnica,
    Costo,
    PrecioVenta,
    Existencia,
    Estado,
    Serie
}

// Parameter object (requerimiento 0.1): agrupa todos los criterios de búsqueda de la Lista de
// Obras en un solo tipo en vez de una firma con diez parámetros sueltos. Todos opcionales:
// ausente = ese filtro no se aplica.
public record ObraFiltro(
    string? TextoLibre = null,
    int? ArtistaId = null,
    int? RubroId = null,
    int? TecnicaId = null,
    Moneda? Moneda = null,
    bool? TieneIVA = null,
    bool? SoloConStock = null,
    EstadoObra? Estado = null,
    decimal? PrecioMinimo = null,
    decimal? PrecioMaximo = null,
    DateOnly? FechaDesde = null,
    DateOnly? FechaHasta = null,
    int? SerieId = null,
    OrdenObra Orden = OrdenObra.FechaIngreso,
    bool OrdenDescendente = true);

// Lo que se ofrece cuando el nombre tipeado coincide con una obra ya cargada al mismo artista
// (decisión #6, docs/CONTEXTO.md): en vez de crear una obra nueva, se suma existencia a esta.
public record ObraCoincidente(int Id, string CodigoVisible, string Titulo, int Existencia, decimal Costo);

public record CrearObraRequest(
    int ArtistaId,
    string Titulo,
    int? RubroId,
    int? TecnicaId,
    decimal? AltoCm,
    decimal? AnchoCm,
    decimal? LargoCm,
    int Existencia,
    Moneda Moneda,
    decimal Costo,
    decimal Utilidad,
    bool TieneIVA,
    decimal PrecioVenta,
    bool PagoContado,
    string? Observaciones);

public record ParametrosCalculoPrecio(decimal IvaPorcentaje, decimal UtilidadDefault, decimal RedondeoPesos, decimal RedondeoDolar)
{
    public decimal RedondeoPara(Moneda moneda) => moneda == Moneda.Pesos ? RedondeoPesos : RedondeoDolar;
}

// Moneda, Existencia y Estado no viajan acá como editables: la moneda queda fija desde el alta
// (decisión #13 — pesos y dólares nunca se mezclan) y la existencia solo cambia por movimientos
// de stock (venta, retiro, alquiler), nunca por edición libre de la ficha.
public record ObraFicha(
    int Id,
    string CodigoVisible,
    int ArtistaId,
    string ArtistaNombre,
    string Titulo,
    int? RubroId,
    int? TecnicaId,
    decimal? AltoCm,
    decimal? AnchoCm,
    decimal? LargoCm,
    int Existencia,
    Moneda Moneda,
    decimal Costo,
    decimal Utilidad,
    bool TieneIVA,
    decimal PrecioVenta,
    bool PagoContado,
    string? Observaciones,
    EstadoObra Estado,
    DateOnly FechaIngreso,
    string? SerieNombre,
    int? SeriePosicion,
    int? SerieTotal,
    string? ImagenUrl,
    DateOnly? FechaEtiquetaImpresa);

// Historial de movimientos (requerimiento 3.1, pestaña de la ficha): lee directo del libro de
// stock que ya alimentan Ventas, Retiros, Alquileres y Devoluciones.
public record MovimientoItem(DateOnly Fecha, TipoMovimiento Tipo, int Cantidad, int? ReferenciaId);

public record ActualizarObraRequest(
    int Id,
    string Titulo,
    int? RubroId,
    int? TecnicaId,
    decimal? AltoCm,
    decimal? AnchoCm,
    decimal? LargoCm,
    decimal Costo,
    decimal Utilidad,
    bool TieneIVA,
    decimal PrecioVenta,
    bool PagoContado,
    string? Observaciones);
