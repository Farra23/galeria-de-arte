namespace Galeria.Application.Common;

// Abstrae quién es el usuario logueado sin que Application dependa de ASP.NET Identity (DIP):
// la implementación real vive en Galeria.Web, que sí conoce ClaimsPrincipal/AuthenticationState.
public interface ICurrentUserService
{
    Task<string> ObtenerUsuarioIdAsync();

    Task<string> ObtenerNombreUsuarioAsync();
}
