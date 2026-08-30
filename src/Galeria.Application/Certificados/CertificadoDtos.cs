namespace Galeria.Application.Certificados;

// Solo los datos que pide el requerimiento 5.3: nunca precio ni costo — es lo que se lleva
// el comprador, no un documento interno.
public record CertificadoDatos(
    int NumeroCertificado,
    DateOnly FechaEmision,
    string CodigoObra,
    string Titulo,
    string ArtistaNombre,
    string? Rubro,
    string? Tecnica,
    decimal? AltoCm,
    decimal? AnchoCm,
    decimal? LargoCm,
    int AnioIngreso,
    string? ImagenUrl);
