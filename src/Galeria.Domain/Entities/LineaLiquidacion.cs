namespace Galeria.Domain.Entities;

public class LineaLiquidacion
{
    public int Id { get; set; }
    public int LiquidacionId { get; set; }
    public Enums.TipoLinea Tipo { get; set; }
    public int? ObraId { get; set; }
    public DateOnly Fecha { get; set; }

    // Desnormalizado para que el PDF no dependa de joins y sea inmutable
    public string CodigoObra { get; set; } = string.Empty;
    public string NombreObra { get; set; } = string.Empty;

    public int Cantidad { get; set; }
    public decimal MontoUnitario { get; set; }
    public decimal MontoTotal { get; set; }
    public string? Observaciones { get; set; }
    public int? ReferenciaId { get; set; }  // Id de Venta, Adelanto, etc.

    public Liquidacion Liquidacion { get; set; } = null!;
    public Obra? Obra { get; set; }
}