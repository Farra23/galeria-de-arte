namespace Galeria.Domain.Entities;

public class Venta
{
    public int Id { get; set; }
    public int ObraId { get; set; }
    public DateOnly Fecha { get; set; }
    public int Cantidad { get; set; } = 1;
    public Enums.Moneda Moneda { get; set; }
    public decimal PrecioVenta { get; set; }      // precio negociado real
    public decimal PrecioCalculado { get; set; }  // precio que arrojó la fórmula
    public bool ExentaIVA { get; set; }
    public string? Observaciones { get; set; }

    public Obra Obra { get; set; } = null!;
    public Certificado? Certificado { get; set; }
    public Devolucion? Devolucion { get; set; }

    // GRASP Information Expert: la venta conoce si hubo negociación de precio
    public decimal DesvioEnPrecio => PrecioVenta - PrecioCalculado;
    public bool HuboDesvio => DesvioEnPrecio != 0;
}