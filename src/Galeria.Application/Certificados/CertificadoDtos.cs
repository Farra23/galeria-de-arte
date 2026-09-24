namespace Galeria.Application.Certificados;

// Solo los datos que pide el requerimiento 5.3: nunca precio ni costo — es lo que se lleva
// el comprador, no un documento interno.
// NumeroCertificado nulo = vista previa (item 13 del testeo del cliente: imprimible desde la
// ficha de la obra, sin pasar por una venta) — no consume numeración correlativa ni queda
// asentado en Certificados, a diferencia del certificado oficial que emite una venta.
public record CertificadoDatos(
    int? NumeroCertificado,
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
