using Galeria.Domain.Enums;

namespace Galeria.Application.Liquidaciones;

// Una línea todavía sin confirmar — la misma forma sirve para la vista previa (no persistida)
// y para volcarse a LineaLiquidacion cuando se confirma (requerimiento 10.4: vista previa
// obligatoria antes de un paso irreversible).
public record LineaPendiente(
    TipoLinea Tipo,
    DateOnly Fecha,
    int? ObraId,
    string CodigoObra,
    string NombreObra,
    int Cantidad,
    decimal MontoUnitario,
    decimal MontoTotal,
    string? Observaciones,
    int? ReferenciaId);

public record VistaPreviaLiquidacion(
    int ArtistaId,
    string ArtistaNombre,
    Moneda Moneda,
    DateOnly? UltimaLiquidacion,
    List<LineaPendiente> Lineas,
    decimal TotalBruto,
    decimal TotalAdelantos,
    decimal TotalDevoluciones,
    decimal TotalNeto);

public record LiquidacionListItem(
    int Id,
    int NumeroCorrelativo,
    DateOnly Fecha,
    string ArtistaNombre,
    Moneda Moneda,
    decimal TotalNeto);

public record LiquidacionDetalle(
    int Id,
    int NumeroCorrelativo,
    DateOnly Fecha,
    int ArtistaId,
    string ArtistaNombre,
    Moneda Moneda,
    List<LineaPendiente> Lineas,
    decimal TotalBruto,
    decimal TotalAdelantos,
    decimal TotalDevoluciones,
    decimal TotalNeto,
    string? ArtistaCorreo,
    string? ArtistaCelular);

public enum OrdenLiquidacion
{
    Numero,
    Fecha,
    Artista,
    Total
}

public record LiquidacionFiltro(
    int? ArtistaId = null,
    DateOnly? FechaDesde = null,
    DateOnly? FechaHasta = null,
    OrdenLiquidacion Orden = OrdenLiquidacion.Numero,
    bool OrdenDescendente = true);
