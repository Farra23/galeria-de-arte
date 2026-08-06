namespace Galeria.Domain.Entities;

public class Auditoria
{
    public long Id { get; set; }  // long: el sistema origen ya tiene 130k registros
    public DateTimeOffset Timestamp { get; set; }
    public string UsuarioId { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;  // desnormalizado
    public string Pantalla { get; set; } = string.Empty;
    public string TipoOperacion { get; set; } = string.Empty;  // Alta | Modificacion | Baja
    public string Tabla { get; set; } = string.Empty;
    public string? Columna { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public int? ArtistaId { get; set; }
    public int? ObraId { get; set; }
    public string? EntidadId { get; set; }
}