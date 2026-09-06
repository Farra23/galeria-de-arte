using System.Security.Claims;
using Galeria.Application.Common;
using Microsoft.AspNetCore.Components.Authorization;

namespace Galeria.Web.Data;

// Implementación real de ICurrentUserService (Application no puede depender de ASP.NET Identity).
// Dos caminos posibles para saber quién es el usuario, según quién llama:
// - Un endpoint Minimal API (ej. los que generan PDF) es un request HTTP normal de punta a punta:
//   ahí SÍ existe HttpContext.User, poblado por el middleware de autenticación.
// - Un componente Blazor Server interactivo corre sobre un circuito de SignalR persistente: pasado
//   el request inicial, HttpContext ya no existe, así que ahí hace falta AuthenticationStateProvider.
// Se prueba HttpContext primero (más directo) y se cae a AuthenticationStateProvider si no hay uno
// autenticado disponible — así un mismo service sirve para los dos casos sin duplicar lógica.
public class CurrentUserService(AuthenticationStateProvider authenticationStateProvider, IHttpContextAccessor httpContextAccessor) : ICurrentUserService
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
        var usuarioDelRequest = httpContextAccessor.HttpContext?.User;
        if (usuarioDelRequest?.Identity?.IsAuthenticated == true)
        {
            return usuarioDelRequest;
        }

        var estado = await authenticationStateProvider.GetAuthenticationStateAsync();
        return estado.User;
    }
}
