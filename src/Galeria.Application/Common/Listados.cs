namespace Galeria.Application.Common;

/// <summary>
/// Tope duro de filas que devuelven las listas grandes (Obras, Ventas, Auditoría). Con el volumen
/// real de la galería (14.000+ obras y ventas) una lista sin filtro intentaba renderizar todo de
/// una y colgaba el navegador.
///
/// Es una red de seguridad, no paginación de verdad: si se llega al tope, la pantalla avisa
/// "mostrando las primeras N, filtrá para ver el resto". La paginación real (server-side, con
/// número de página) queda como mejora pendiente.
/// </summary>
public static class Listados
{
    public const int Tope = 500;
}
