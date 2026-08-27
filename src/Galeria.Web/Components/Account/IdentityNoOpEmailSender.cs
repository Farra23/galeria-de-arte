using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Galeria.Web.Data;

namespace Galeria.Web.Components.Account;

// Implementación requerida por AddIdentityCore<ApplicationUser>. No hay registro público ni
// recuperación de contraseña por mail en este sistema (los da de alta un administrador desde
// Configuración > Usuarios), así que estos métodos no se invocan en la práctica.
internal sealed class IdentityNoOpEmailSender : IEmailSender<ApplicationUser>
{
    private readonly IEmailSender emailSender = new NoOpEmailSender();

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        emailSender.SendEmailAsync(email, "Confirmá tu correo", $"Confirmá tu cuenta haciendo <a href='{confirmationLink}'>clic acá</a>.");

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        emailSender.SendEmailAsync(email, "Restablecé tu contraseña", $"Restablecé tu contraseña haciendo <a href='{resetLink}'>clic acá</a>.");

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        emailSender.SendEmailAsync(email, "Restablecé tu contraseña", $"Restablecé tu contraseña usando el siguiente código: {resetCode}");
}
