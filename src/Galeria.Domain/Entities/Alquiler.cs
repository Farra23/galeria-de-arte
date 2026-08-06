namespace Galeria.Domain.Entities;

public class Alquiler
{
    public int Id { get; set; }
    public int ObraId { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaRecupero { get; set; }    // fecha prevista de devolución
    public DateOnly? FechaDevolucion { get; set; }  // fecha real de devolución
    public Enums.Moneda Moneda { get; set; }
    public decimal PorcentajeAlquiler { get; set; } // % sobre PrecioVenta de la obra
    public decimal MontoAlquiler { get; set; }      // monto mensual total
    public decimal MontoArtista { get; set; }
    public decimal MontoGaleria { get; set; }
    public string? Cliente { get; set; }

    public Obra Obra { get; set; } = null!;

    // GRASP Information Expert
    public bool EstaActivo => !FechaDevolucion.HasValue;

    public bool EstaVencido =>
        EstaActivo &&
        FechaRecupero.HasValue &&
        FechaRecupero.Value < DateOnly.FromDateTime(DateTime.Today);
}