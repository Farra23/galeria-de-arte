namespace Galeria.Domain.Entities;

// Estado de seguimiento del artista en la Agenda de pagos (requerimiento 11): a diferencia del
// resto de la agenda (piezas del mes, plata pendiente), la fecha pactada, la confirmada y los
// comentarios son datos que alguien escribe a mano y no se pueden derivar de ninguna otra tabla.
// Un registro por artista (no por período): representa el llamado en curso, no un historial.
public class AgendaPago
{
    public int Id { get; set; }
    public int ArtistaId { get; set; }
    public DateOnly? FechaPactada { get; set; }
    public DateOnly? FechaConfirmada { get; set; }
    public string? Comentarios { get; set; }

    public Artista Artista { get; set; } = null!;
}
