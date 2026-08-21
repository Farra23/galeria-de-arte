using System.Security.Claims;
using Galeria.Application.Common;
using Microsoft.AspNetCore.Components.Authorization;

namespace Galeria.Web.Data;

// Implementación real de ICurrentUserService (Application no puede depender de ASP.NET Identity).
// Usa AuthenticationStateProvider en vez de IHttpContextAccessor: en Blazor Server interactivo el
// HttpContext solo existe durante el request inicial, no en el circuito de SignalR persistente.
public class CurrentUserService(AuthenticationStateProvider authenticationStateProvider) : ICurrentUserService
{
    public async Task<string> ObtenerUsuarioIdAsync()
    {
        var usuario = await ObtenerUsuarioAsync();
        return usuario.FindFirstValue(ClaimTypes.NameIdentifier) ?? "desconocido";
    }

    public async Task<string> ObtenerNombreUsuarioAsync()
    {
        var usuario = await ObtenerUsuarioAsync();
        return usuario.Identity?.Name ?? "desconocido";
    }

    private async Task<ClaimsPrincipal> ObtenerUsuarioAsync()
    {
        var estado = await authenticationStateProvider.GetAuthenticationStateAsync();
        return estado.User;
    }
}
