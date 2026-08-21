using Galeria.Domain.Enums;

namespace Galeria.Application.Retiros;

public record RetiroListItem(
    int Id,
    DateOnly Fecha,
    string CodigoObra,
    string Titulo,
    string ArtistaNombre,
    TipoRetiro Tipo,
    string? Motivo,
    bool EstaDevuelto,
    DateOnly? FechaEstimadaDevolucion,
    bool EstaVencido);

public record RegistrarRetiroRequest(
    int ObraId,
    DateOnly Fecha,
    int Cantidad,
    TipoRetiro Tipo,
    string? Motivo,
    DateOnly? FechaEstimadaDevolucion);

public record RetiroFiltro(
    string? TextoLibre = null,
    int? ArtistaId = null,
    TipoRetiro? Tipo = null,
    bool? SoloActivos = null);
