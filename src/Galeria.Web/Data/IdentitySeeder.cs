using Microsoft.AspNetCore.Identity;

namespace Galeria.Web.Data;

// V1 tiene un solo usuario con todos los permisos (decisión #11, docs/CONTEXTO.md) y el registro
// público está deshabilitado (se borró Account/Register). Este seeder es la única forma de que
// exista un usuario: crea el admin una sola vez, a partir de configuración — nunca hardcodeado.
// El login es usuario + contraseña (sin mail): es el candado de la PC, no una cuenta con recupero.
public static class IdentitySeeder
{
    public static async Task SeedAdminAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (userManager.Users.Any())
        {
            return;
        }

        var userName = configuration["AdminSeed:UserName"]
            ?? throw new InvalidOperationException(
                "Falta 'AdminSeed:UserName'. En desarrollo: dotnet user-secrets set \"AdminSeed:UserName\" \"...\" --project src/Galeria.Web");
        var password = configuration["AdminSeed:Password"]
            ?? throw new InvalidOperationException(
                "Falta 'AdminSeed:Password'. En desarrollo: dotnet user-secrets set \"AdminSeed:Password\" \"...\" --project src/Galeria.Web");

        var admin = new ApplicationUser { UserName = userName };

        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"No se pudo crear el usuario admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}
