namespace Galeria.Domain.Entities;

// Registro interno del libro de stock. No tiene pantalla propia —
// es la fuente que alimenta la Auditoría y permite reconstruir el stock.
public class Movimiento
{
    public int Id { get; set; }
    public int ObraId { get; set; }
    public DateOnly Fecha { get; set; }
    public Enums.TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public Enums.Moneda Moneda { get; set; }
    public int? ReferenciaId { get; set; }  // Id de Venta, Retiro, Alquiler, etc.

    public Obra Obra { get; set; } = null!;
}