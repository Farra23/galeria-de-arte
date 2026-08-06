namespace Galeria.Domain.Entities;

public class Serie
{
    public int Id { get; set; }
    public int ArtistaId { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public Artista Artista { get; set; } = null!;
    public ICollection<Obra> Obras { get; set; } = [];
}