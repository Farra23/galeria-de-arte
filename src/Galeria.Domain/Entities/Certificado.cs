namespace Galeria.Domain.Entities;

public class Certificado
{
    public int Id { get; set; }
    public int NumeroCertificado { get; set; }
    public int VentaId { get; set; }
    public DateOnly FechaEmision { get; set; }
    public string? EmailDestinatario { get; set; }
    public string? PdfPath { get; set; }

    public Venta Venta { get; set; } = null!;
}