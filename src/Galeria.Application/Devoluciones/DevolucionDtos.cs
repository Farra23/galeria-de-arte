using Galeria.Domain.Enums;

namespace Galeria.Application.Devoluciones;

public record VentaParaDevolucion(
    int VentaId,
    DateOnly Fecha,
    string CodigoObra,
    string Titulo,
    string ArtistaNombre,
    int Cantidad,
    Moneda Moneda,
    decimal PrecioVenta);

public record RegistrarDevolucionRequest(
    int VentaId,
    DateOnly Fecha,
    string? Motivo,
    bool ArtistaYaCobro);

public record DevolucionListItem(
    int Id,
    DateOnly Fecha,
    string CodigoObra,
    string Titulo,
    string ArtistaNombre,
    string? Motivo,
    bool ArtistaYaCobro);

public enum OrdenDevolucion
{
    Fecha,
    Codigo,
    Obra,
    Artista,
    Motivo
}

public record DevolucionFiltro(
    string? TextoLibre = null,
    int? ArtistaId = null,
    OrdenDevolucion Orden = OrdenDevolucion.Fecha,
    bool OrdenDescendente = true);
