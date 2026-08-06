namespace Galeria.Domain.Entities;

public class Adelanto
{
    public int Id { get; set; }
    public int ArtistaId { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal Importe { get; set; }
    public Enums.Moneda Moneda { get; set; }
    public Enums.TipoAdelanto Tipo { get; set; }
    public string? Observaciones { get; set; }
    public DateOnly? FechaDescontado { get; set; }
    public int? LiquidacionId { get; set; }

    public Artista Artista { get; set; } = null!;
    public Liquidacion? Liquidacion { get; set; }

    public bool FueDescontado => LiquidacionId.HasValue;

    // GRASP Information Expert: el adelanto conoce su impacto en la liquidación.
    // Adelanto resta; AjusteAFavor suma.
    public decimal ImpactoEnLiquidacion =>
        Tipo == Enums.TipoAdelanto.Adelanto ? -Importe : Importe;
}