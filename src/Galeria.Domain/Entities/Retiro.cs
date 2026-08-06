namespace Galeria.Domain.Entities;

public class Retiro
{
    public int Id { get; set; }
    public int ObraId { get; set; }
    public DateOnly Fecha { get; set; }
    public int Cantidad { get; set; }
    public Enums.TipoRetiro Tipo { get; set; }
    public string? Motivo { get; set; }
    public DateOnly? FechaEstimadaDevolucion { get; set; }
    public DateOnly? FechaDevolucion { get; set; }  // null = todavía fuera

    public Obra Obra { get; set; } = null!;

    // GRASP Information Expert
    public bool EstaDevuelto => FechaDevolucion.HasValue;

    public bool EstaVencido =>
        Tipo == Enums.TipoRetiro.Temporal &&
        FechaEstimadaDevolucion.HasValue &&
        !EstaDevuelto &&
        FechaEstimadaDevolucion.Value < DateOnly.FromDateTime(DateTime.Today);
}