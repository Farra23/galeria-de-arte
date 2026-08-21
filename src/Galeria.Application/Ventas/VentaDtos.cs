using Galeria.Domain.Enums;

namespace Galeria.Application.Ventas;

public record RegistrarVentaRequest(
    int ObraId,
    DateOnly Fecha,
    int Cantidad,
    Moneda Moneda,
    decimal PrecioVenta,
    bool ExentaIVA,
    string? Observaciones);

public record VentaListItem(
    int Id,
    DateOnly Fecha,
    string CodigoObra,
    string Titulo,
    string ArtistaNombre,
    int Cantidad,
    Moneda Moneda,
    decimal PrecioVenta,
    bool TieneCertificado);

// Parameter object (mismo patrón que ObraFiltro en Obras/ObraDtos.cs).
public record VentaFiltro(
    string? TextoLibre = null,
    int? ArtistaId = null,
    Moneda? Moneda = null,
    DateOnly? FechaDesde = null,
    DateOnly? FechaHasta = null);
