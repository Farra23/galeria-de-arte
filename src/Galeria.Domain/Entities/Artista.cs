namespace Galeria.Domain.Entities;

public class Artista
{
    public int Id { get; set; }
    public int Codigo { get; set; }
    public string Apellido { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Perfil { get; set; }
    public string? Taller { get; set; }
    public string? Celular { get; set; }
    public string? TelFijo { get; set; }
    public string? Direccion { get; set; }
    public string? Correo { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Obra> Obras { get; set; } = [];
    public ICollection<Serie> Series { get; set; } = [];
    public ICollection<Adelanto> Adelantos { get; set; } = [];
    public ICollection<Liquidacion> Liquidaciones { get; set; } = [];

    // GRASP Information Expert: el artista conoce su propio nombre formateado
    public string NombreCompleto => $"{Apellido}, {Nombre}";
}