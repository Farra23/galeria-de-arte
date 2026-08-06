namespace Galeria.Domain.Entities;

public class Liquidacion
{
    public int Id { get; set; }
    public int ArtistaId { get; set; }
    public int NumeroCorrelativo { get; set; }
    public DateOnly Fecha { get; set; }
    public int PeriodoAnio { get; set; }
    public int PeriodoMes { get; set; }
    public Enums.Moneda Moneda { get; set; }
    public Enums.EstadoLiquidacion Estado { get; set; } = Enums.EstadoLiquidacion.Borrador;
    public decimal TotalBruto { get; set; }
    public decimal TotalAdelantos { get; set; }
    public decimal TotalDevoluciones { get; set; }
    public decimal TotalNeto { get; set; }
    public string? PdfPath { get; set; }

    public Artista Artista { get; set; } = null!;
    public ICollection<LineaLiquidacion> Lineas { get; set; } = [];
    public ICollection<Adelanto> Adelantos { get; set; } = [];

    public bool EsConfirmada => Estado == Enums.EstadoLiquidacion.Confirmada;
}