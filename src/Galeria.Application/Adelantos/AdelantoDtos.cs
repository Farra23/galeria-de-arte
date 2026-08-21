using Galeria.Domain.Enums;

namespace Galeria.Application.Adelantos;

public record AdelantoListItem(
    int Id,
    DateOnly Fecha,
    string ArtistaNombre,
    Moneda Moneda,
    decimal Importe,
    TipoAdelanto Tipo,
    string? Observaciones,
    bool FueDescontado,
    int? LiquidacionId);

public record RegistrarAdelantoRequest(
    int ArtistaId,
    DateOnly Fecha,
    Moneda Moneda,
    decimal Importe,
    TipoAdelanto Tipo,
    string? Observaciones);

public record AdelantoFiltro(
    int? ArtistaId = null,
    DateOnly? FechaDesde = null,
    DateOnly? FechaHasta = null);
