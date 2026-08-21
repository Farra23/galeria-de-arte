namespace Galeria.Application.Catalogos;

public record OpcionCatalogo(int Id, string Nombre);

public record CatalogoItem(int Id, string Nombre, bool Activo);
