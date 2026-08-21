using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Galeria.Application.Adelantos;
using Galeria.Application.Alquileres;
using Galeria.Application.Artistas;
using Galeria.Application.Auditorias;
using Galeria.Application.Catalogos;
using Galeria.Application.Certificados;
using Galeria.Application.Common;
using Galeria.Application.Devoluciones;
using Galeria.Application.Obras;
using Galeria.Application.Parametros;
using Galeria.Application.Retiros;
using Galeria.Application.Ventas;
using Galeria.Infrastructure.Persistence;
using Galeria.Infrastructure.Persistence.Repositories;
using Galeria.Web.Components;
using Galeria.Web.Components.Account;
using Galeria.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Mismo archivo .db que Identity (decisión de stack: un solo archivo = un solo backup para el cliente),
// pero DbContext separado: GaleriaDbContext no sabe nada de autenticación, y viceversa (SRP).
builder.Services.AddDbContext<GaleriaDbContext>(options =>
    options.UseSqlite(connectionString));

// Repositorios (Infrastructure implementa las interfaces que define Application — DIP)
// y servicios de aplicación, uno por caso de uso.
builder.Services.AddScoped<IArtistaRepository, ArtistaRepository>();
builder.Services.AddScoped<IObraRepository, ObraRepository>();
builder.Services.AddScoped<IParametroRepository, ParametroRepository>();
builder.Services.AddScoped<ICatalogoRepository, CatalogoRepository>();
builder.Services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<ICertificadoRepository, CertificadoRepository>();
builder.Services.AddScoped<IRetiroRepository, RetiroRepository>();
builder.Services.AddScoped<IAlquilerRepository, AlquilerRepository>();
builder.Services.AddScoped<IAdelantoRepository, AdelantoRepository>();
builder.Services.AddScoped<IDevolucionRepository, DevolucionRepository>();
builder.Services.AddScoped<ArtistaService>();
builder.Services.AddScoped<ObraService>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddScoped<VentaService>();
builder.Services.AddScoped<CertificadoService>();
builder.Services.AddScoped<RetiroService>();
builder.Services.AddScoped<AlquilerService>();
builder.Services.AddScoped<AdelantoService>();
builder.Services.AddScoped<DevolucionService>();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        // Sin registro público y sin mail real (login = usuario + contraseña), "cuenta confirmada"
        // no es un concepto que exista en esta app.
        options.SignIn.RequireConfirmedAccount = false;

        // Cuenta bloqueada tras intentos fallidos — clave porque hay un solo usuario admin
        // y la app va a estar expuesta en la red local de la galería.
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

        // Política de contraseña aflojada a propósito: login local de un solo usuario, sin
        // exposición a internet — no un servicio público. Si eso cambia (acceso remoto, más
        // usuarios), esto es lo primero que hay que endurecer.
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Cierre de sesión por inactividad (requerimiento #1: "que no quede abierto en la PC del mostrador").
// Sliding: cada request activo renueva el plazo; 30 min sin uso y hay que loguearse de nuevo.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

// Rate limiting: primera defensa contra fuerza bruta sobre /Account/Login. Es un límite global
// por IP y no solo sobre el login porque en Blazor Server las interacciones normales viajan por
// el circuito de SignalR, no por HTTP — la superficie HTTP real son la carga de página y los
// formularios con post-back (login incluido), así que limitar a nivel de app ya cubre el caso.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

await IdentitySeeder.SeedAdminAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Todo en español rioplatense (requerimiento 0.7): fechas dd/MM/aaaa, miles con punto,
// decimales con coma. Cultura fija, no depende del navegador de quien entre.
var culturaFija = new[] { new CultureInfo("es-UY") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("es-UY"),
    SupportedCultures = culturaFija,
    SupportedUICultures = culturaFija
});

app.UseHttpsRedirection();

// Cabeceras de seguridad básicas en toda respuesta.
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    await next();
});

app.UseStaticFiles();
app.UseRateLimiter();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
