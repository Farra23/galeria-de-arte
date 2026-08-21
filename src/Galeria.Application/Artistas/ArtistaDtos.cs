namespace Galeria.Application.Artistas;

public record ArtistaListItem(
    int Id,
    int Codigo,
    string NombreCompleto,
    string? Taller,
    string? Celular,
    string? Correo,
    int CantidadObras,
    int ObrasEnStock,
    decimal SaldoPesos,
    decimal SaldoDolares);

public enum OrdenArtista
{
    Nombre,
    Codigo,
    Taller,
    CantidadObras,
    ObrasEnStock,
    SaldoPesos,
    SaldoDolares
}

// Parameter object (mismo patrón que ObraFiltro): agrupa texto libre + orden de la Lista de
// Artistas (requerimiento 0.1, orden por cualquier columna).
public record ArtistaFiltro(
    string? TextoLibre = null,
    OrdenArtista Orden = OrdenArtista.Nombre,
    bool OrdenDescendente = false);

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

// Código no viaja como editable: es la clave visible del artista, ya comunicada/impresa
// en sus obras — cambiarla post-alta rompería esa referencia.
public record ArtistaFicha(
    int Id,
    int Codigo,
    string Apellido,
    string Nombre,
    string? Taller,
    string? Celular,
    string? TelFijo,
    string? Direccion,
    string? Correo,
    string? Perfil,
    int CantidadObras);

public record ActualizarArtistaRequest(
    int Id,
    string Apellido,
    string Nombre,
    string? Taller,
    string? Celular,
    string? TelFijo,
    string? Direccion,
    string? Correo,
    string? Perfil);
