using Galeria.Domain.Enums;

namespace Galeria.Application.Alquileres;

public record AlquilerListItem(
    int Id,
    DateOnly FechaInicio,
    string CodigoObra,
    string Titulo,
    string ArtistaNombre,
    string? Cliente,
    Moneda Moneda,
    decimal MontoAlquiler,
    decimal MontoArtista,
    bool EstaActivo,
    bool EstaVencido,
    DateOnly? FechaRecupero);

// El alquiler se calcula en dos pasos (requerimiento 7.2):
// 1) PorcentajeAlquiler sobre el precio de venta de la obra → importe total del alquiler.
// 2) PorcentajeArtista sobre ese importe → cuánto de eso le corresponde al artista (el resto, galería).
public record RegistrarAlquilerRequest(
    int ObraId,
    DateOnly FechaInicio,
    DateOnly? FechaRecupero,
    decimal PorcentajeAlquiler,
    decimal PorcentajeArtista,
    string? Cliente);

public record AlquilerFiltro(
    string? TextoLibre = null,
    int? ArtistaId = null,
    bool? SoloActivos = null);
