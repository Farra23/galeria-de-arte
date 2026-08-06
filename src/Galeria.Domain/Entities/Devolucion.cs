namespace Galeria.Domain.Entities;

public class Devolucion
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public DateOnly Fecha { get; set; }
    public string? Motivo { get; set; }

    // Si el artista ya cobró → el importe queda como adelanto a descontar.
    // Si no cobró → aparece en la liquidación como línea Devolución con monto 0.
    public bool ArtistaYaCobro { get; set; }

    public Venta Venta { get; set; } = null!;
}