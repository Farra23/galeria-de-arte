namespace Galeria.Application.Auditorias;

public record AuditoriaListItem(
    long Id,
    DateTimeOffset Timestamp,
    string NombreUsuario,
    string Pantalla,
    string TipoOperacion,
    string Tabla,
    string? Columna,
    string? ValorAnterior,
    string? ValorNuevo,
    int? ArtistaId,
    int? ObraId,
    string? EntidadId);

// Parameter object (mismo patrón que ObraFiltro): agrupa los filtros de la pantalla de Auditoría
// (requerimiento 13, FILTRABLE en mayúsculas en el pedido original del cliente).
public record AuditoriaFiltro(
    DateOnly? FechaDesde = null,
    DateOnly? FechaHasta = null,
    string? Usuario = null,
    string? Pantalla = null,
    string? TipoOperacion = null);

public record RegistrarAuditoriaRequest(
    string Pantalla,
    string TipoOperacion,
    string Tabla,
    string? Columna = null,
    string? ValorAnterior = null,
    string? ValorNuevo = null,
    int? ArtistaId = null,
    int? ObraId = null,
    string? EntidadId = null);
