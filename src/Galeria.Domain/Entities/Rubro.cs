namespace Galeria.Domain.Entities;

public class Rubro
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public ICollection<Obra> Obras { get; set; } = [];
}