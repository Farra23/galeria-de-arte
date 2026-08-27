using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Galeria.Web.Data;

namespace Microsoft.AspNetCore.Routing;

// Endpoints requeridos por los componentes Razor de Identity en /Components/Account/Pages.
// Se sacaron los de login externo y descarga de datos personales (self-service GDPR) porque
// no hay proveedores externos configurados ni sentido de "mis datos" en un sistema de un solo
// usuario admin — las páginas que los usaban se eliminaron (ver auditoría 2026-08-27).
internal static class IdentityComponentsEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/Account");

        accountGroup.MapPost("/Logout", async (
            ClaimsPrincipal user,
            SignInManager<ApplicationUser> signInManager,
            [FromForm] string returnUrl) =>
        {
            await signInManager.SignOutAsync();
            return TypedResults.LocalRedirect($"~/{returnUrl}");
        });

        return accountGroup;
    }
}
