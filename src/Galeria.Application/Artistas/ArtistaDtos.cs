namespace Galeria.Application.Artistas;

public record ArtistaListItem(
    int Id,
    int Codigo,
    string NombreCompleto,
    string? Taller,
    string? Celular,
    string? Correo,
    int CantidadObras);

// Versión liviana para desplegables (alta de Obra) — no necesita todos los datos de la ficha.
public record ArtistaOpcion(int Id, int Codigo, string NombreCompleto);

public record CrearArtistaRequest(
    string Apellido,
    string Nombre,
    string? Taller,
    string? Celular,
    string? TelFijo,
    string? Direccion,
    string? Correo,
    string? Perfil);
