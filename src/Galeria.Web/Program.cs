using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Galeria.Application.Adelantos;
using Galeria.Application.Agenda;
using Galeria.Application.Alquileres;
using Galeria.Application.Artistas;
using Galeria.Application.Auditorias;
using Galeria.Application.Catalogos;
using Galeria.Application.Certificados;
using Galeria.Application.Common;
using Galeria.Application.Devoluciones;
using Galeria.Domain.Entities;
using Galeria.Domain.Enums;
using Galeria.Application.Liquidaciones;
using Galeria.Application.Obras;
using Galeria.Application.Parametros;
using Galeria.Application.Resumen;
using Galeria.Application.Retiros;
using Galeria.Application.Ventas;
using Galeria.Infrastructure.Persistence;
using Galeria.Infrastructure.Persistence.Repositories;
using Galeria.Web.Components;
using Galeria.Web.Components.Account;
using Galeria.Web.Data;
using Galeria.Web.Pdf;

var builder = WebApplication.CreateBuilder(args);

// Permite que la app corra como Servicio de Windows en la PC de la galería (arranque automático,
// sobrevive a un reinicio). Es un no-op cuando NO corre como servicio (ej. `dotnet run` en
// desarrollo), así que no cambia nada del flujo normal de trabajo.
builder.Host.UseWindowsService();

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
builder.Services.AddScoped<ILiquidacionRepository, LiquidacionRepository>();
builder.Services.AddScoped<IAgendaPagoRepository, AgendaPagoRepository>();
builder.Services.AddScoped<ArtistaService>();
builder.Services.AddScoped<ObraService>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddScoped<VentaService>();
builder.Services.AddScoped<CertificadoService>();
builder.Services.AddScoped<RetiroService>();
builder.Services.AddScoped<AlquilerService>();
builder.Services.AddScoped<AdelantoService>();
builder.Services.AddScoped<DevolucionService>();
builder.Services.AddScoped<LiquidacionService>();
builder.Services.AddScoped<ResumenService>();
builder.Services.AddScoped<AgendaService>();

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

// Requerimiento 0.2 (INSISTIDO): PDF real para comprobantes y listas, no solo "imprimir" del
// navegador. QuestPDF es Community (gratis) para una empresa de este tamaño — sin dependencias
// externas (no arranca un Chromium como haría un enfoque basado en HTML-a-PDF).
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

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

// Sin HTTPS a propósito: se instala en una sola PC de la galería (o a lo sumo su red local),
// sin certificado ni exposición a internet — forzar el redirect acá solo generaría una advertencia
// en cada arranque (no hay puerto https configurado) sin aportar seguridad real en ese escenario.
// Si el día de mañana esto se expone fuera de la red local, es lo primero que hay que revertir.

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

// Requerimiento 0.2 (INSISTIDO): PDF real de los comprobantes, no solo "imprimir" del navegador.
// Endpoints propios (no Razor components) porque generar el PDF es un detalle de infraestructura
// de Web, no algo que tenga sentido meter en la página que ya renderiza la vista imprimible.
app.MapGet("/certificados/{certificadoId:int}/pdf", async (int certificadoId, CertificadoService certificados, IParametroRepository parametros, IWebHostEnvironment env) =>
{
    var datos = await certificados.ObtenerDatosAsync(certificadoId);
    if (datos is null)
    {
        return Results.NotFound();
    }

    var nombreGaleria = await parametros.ObtenerAsync(Parametro.Claves.NombreGaleria) is { Length: > 0 } nombre
        ? nombre
        : "Galería ACATRAS";

    byte[]? imagenBytes = null;
    if (datos.ImagenUrl is not null)
    {
        var rutaFisica = Path.Combine(env.WebRootPath, datos.ImagenUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(rutaFisica))
        {
            imagenBytes = await File.ReadAllBytesAsync(rutaFisica);
        }
    }

    var pdf = CertificadoPdfGenerator.Generar(datos, nombreGaleria, imagenBytes);
    return Results.File(pdf, "application/pdf", $"certificado-{datos.NumeroCertificado:D6}.pdf");
}).RequireAuthorization();

app.MapGet("/liquidaciones/{id:int}/pdf", async (int id, LiquidacionService liquidaciones, IParametroRepository parametros) =>
{
    var detalle = await liquidaciones.ObtenerDetalleAsync(id);
    if (detalle is null)
    {
        return Results.NotFound();
    }

    var valores = await parametros.ObtenerVariosAsync(
    [
        Parametro.Claves.NombreGaleria,
        Parametro.Claves.DireccionGaleria,
        Parametro.Claves.TelefonoGaleria
    ]);

    var nombreGaleria = valores.GetValueOrDefault(Parametro.Claves.NombreGaleria) is { Length: > 0 } nombre
        ? nombre
        : "Galería ACATRAS";

    var pdf = LiquidacionPdfGenerator.Generar(
        detalle, nombreGaleria,
        valores.GetValueOrDefault(Parametro.Claves.DireccionGaleria),
        valores.GetValueOrDefault(Parametro.Claves.TelefonoGaleria));

    return Results.File(pdf, "application/pdf", $"liquidacion-{detalle.NumeroCorrelativo:D6}.pdf");
}).RequireAuthorization();

// Exportar a Excel/CSV (requerimiento 0.2 "+"): cada endpoint respeta el mismo filtro activo que
// su lista, parseado de la querystring con las mismas claves que SupplyParameterFromQuery usa en
// la página — se repite acá porque un endpoint mínimo no tiene ese mecanismo de Blazor disponible.
app.MapGet("/obras/exportar.csv", async (HttpRequest request, ObraService obras) =>
{
    var q = request.Query;
    var filtro = new ObraFiltro(
        TextoLibre: q["q"],
        ArtistaId: QueryHelper.Int(q, "artista"),
        RubroId: QueryHelper.Int(q, "rubro"),
        TecnicaId: QueryHelper.Int(q, "tecnica"),
        Moneda: QueryHelper.Enum<Moneda>(q, "moneda"),
        TieneIVA: QueryHelper.Bool(q, "iva"),
        SoloConStock: QueryHelper.Bool(q, "stock"),
        Estado: QueryHelper.Enum<EstadoObra>(q, "estado"),
        PrecioMinimo: QueryHelper.Decimal(q, "precioMin"),
        PrecioMaximo: QueryHelper.Decimal(q, "precioMax"),
        FechaDesde: QueryHelper.Fecha(q, "desde"),
        FechaHasta: QueryHelper.Fecha(q, "hasta"));

    var resultado = await obras.BuscarAsync(filtro);

    return CsvHelper.Generar("obras.csv",
        ["Codigo", "Nombre", "Artista", "Rubro", "Tecnica", "Moneda", "Costo", "PrecioVenta", "Stock", "Estado", "FechaIngreso"],
        resultado.Select(o => new[]
        {
            o.CodigoVisible, o.Titulo, o.ArtistaNombre, o.Rubro ?? "", o.Tecnica ?? "", o.Moneda.ToString(),
            QueryHelper.Num(o.Costo), QueryHelper.Num(o.PrecioVenta), o.Existencia.ToString(), o.Estado.ToString(),
            o.FechaIngreso.ToString("yyyy-MM-dd")
        }));
}).RequireAuthorization();

app.MapGet("/artistas/exportar.csv", async (HttpRequest request, ArtistaService artistas) =>
{
    var q = request.Query;
    var orden = QueryHelper.Enum<OrdenArtista>(q, "orden") ?? OrdenArtista.Nombre;
    var filtro = new ArtistaFiltro(q["q"], orden, QueryHelper.Bool(q, "desc") ?? false);

    var resultado = await artistas.BuscarAsync(filtro);

    return CsvHelper.Generar("artistas.csv",
        ["Codigo", "Nombre", "Taller", "Celular", "Correo", "Obras", "ObrasEnStock", "SaldoPesos", "SaldoDolares"],
        resultado.Select(a => new[]
        {
            a.Codigo.ToString("D3"), a.NombreCompleto, a.Taller ?? "", a.Celular ?? "", a.Correo ?? "",
            a.CantidadObras.ToString(), a.ObrasEnStock.ToString(), QueryHelper.Num(a.SaldoPesos), QueryHelper.Num(a.SaldoDolares)
        }));
}).RequireAuthorization();

app.MapGet("/ventas/exportar.csv", async (HttpRequest request, VentaService ventas) =>
{
    var q = request.Query;
    var filtro = new VentaFiltro(q["q"], QueryHelper.Int(q, "artista"), QueryHelper.Enum<Moneda>(q, "moneda"));

    var resultado = await ventas.BuscarAsync(filtro);

    return CsvHelper.Generar("ventas.csv",
        ["Fecha", "Codigo", "Obra", "Artista", "Cantidad", "Moneda", "Precio"],
        resultado.Select(v => new[]
        {
            v.Fecha.ToString("yyyy-MM-dd"), v.CodigoObra, v.Titulo, v.ArtistaNombre,
            v.Cantidad.ToString(), v.Moneda.ToString(), QueryHelper.Num(v.PrecioVenta)
        }));
}).RequireAuthorization();

app.MapGet("/retiros/exportar.csv", async (HttpRequest request, RetiroService retiros) =>
{
    var q = request.Query;
    var filtro = new RetiroFiltro(q["q"], QueryHelper.Int(q, "artista"), QueryHelper.Enum<TipoRetiro>(q, "tipo"));

    var resultado = await retiros.BuscarAsync(filtro);

    return CsvHelper.Generar("retiros.csv",
        ["Fecha", "Artista", "Codigo", "Obra", "Tipo", "Motivo", "Devuelto"],
        resultado.Select(r => new[]
        {
            r.Fecha.ToString("yyyy-MM-dd"), r.ArtistaNombre, r.CodigoObra, r.Titulo, r.Tipo.ToString(),
            r.Motivo ?? "", r.EstaDevuelto ? "si" : "no"
        }));
}).RequireAuthorization();

app.MapGet("/alquileres/exportar.csv", async (HttpRequest request, AlquilerService alquileres) =>
{
    var q = request.Query;
    var filtro = new AlquilerFiltro(q["q"], QueryHelper.Int(q, "artista"), QueryHelper.Bool(q, "activos"));

    var resultado = await alquileres.BuscarAsync(filtro);

    return CsvHelper.Generar("alquileres.csv",
        ["Inicio", "Codigo", "Obra", "Artista", "Cliente", "Moneda", "Monto", "MontoArtista", "Activo"],
        resultado.Select(a => new[]
        {
            a.FechaInicio.ToString("yyyy-MM-dd"), a.CodigoObra, a.Titulo, a.ArtistaNombre, a.Cliente ?? "",
            a.Moneda.ToString(), QueryHelper.Num(a.MontoAlquiler), QueryHelper.Num(a.MontoArtista), a.EstaActivo ? "si" : "no"
        }));
}).RequireAuthorization();

app.MapGet("/adelantos/exportar.csv", async (HttpRequest request, AdelantoService adelantos) =>
{
    var q = request.Query;
    var filtro = new AdelantoFiltro(QueryHelper.Int(q, "artista"), QueryHelper.Fecha(q, "desde"), QueryHelper.Fecha(q, "hasta"));

    var resultado = await adelantos.BuscarAsync(filtro);

    return CsvHelper.Generar("adelantos.csv",
        ["Fecha", "Artista", "Tipo", "Moneda", "Monto", "Observaciones", "Descontado"],
        resultado.Select(a => new[]
        {
            a.Fecha.ToString("yyyy-MM-dd"), a.ArtistaNombre, a.Tipo.ToString(), a.Moneda.ToString(),
            QueryHelper.Num(a.Importe), a.Observaciones ?? "", a.FueDescontado ? "si" : "no"
        }));
}).RequireAuthorization();

app.MapGet("/liquidaciones/exportar.csv", async (HttpRequest request, LiquidacionService liquidaciones) =>
{
    var q = request.Query;
    var filtro = new LiquidacionFiltro(QueryHelper.Int(q, "artista"));

    var resultado = await liquidaciones.BuscarAsync(filtro);

    return CsvHelper.Generar("liquidaciones.csv",
        ["Numero", "Fecha", "Artista", "Moneda", "TotalNeto"],
        resultado.Select(l => new[]
        {
            l.NumeroCorrelativo.ToString("D6"), l.Fecha.ToString("yyyy-MM-dd"), l.ArtistaNombre,
            l.Moneda.ToString(), QueryHelper.Num(l.TotalNeto)
        }));
}).RequireAuthorization();

app.MapGet("/devoluciones/exportar.csv", async (HttpRequest request, DevolucionService devoluciones) =>
{
    var q = request.Query;
    var filtro = new DevolucionFiltro(q["q"], QueryHelper.Int(q, "artista"));

    var resultado = await devoluciones.BuscarAsync(filtro);

    return CsvHelper.Generar("devoluciones.csv",
        ["Fecha", "Codigo", "Obra", "Artista", "Motivo", "ArtistaYaCobro"],
        resultado.Select(d => new[]
        {
            d.Fecha.ToString("yyyy-MM-dd"), d.CodigoObra, d.Titulo, d.ArtistaNombre, d.Motivo ?? "",
            d.ArtistaYaCobro ? "si" : "no"
        }));
}).RequireAuthorization();

app.MapGet("/auditoria/exportar.csv", async (HttpRequest request, AuditoriaService auditoria) =>
{
    var q = request.Query;
    var filtro = new AuditoriaFiltro(QueryHelper.Fecha(q, "desde"), QueryHelper.Fecha(q, "hasta"),
        q["usuario"], q["pantalla"], q["tipo"]);

    var resultado = await auditoria.BuscarAsync(filtro);

    return CsvHelper.Generar("auditoria.csv",
        ["FechaHora", "Usuario", "Pantalla", "Operacion", "Tabla", "Campo", "Anterior", "Nuevo"],
        resultado.Select(r => new[]
        {
            r.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), r.NombreUsuario, r.Pantalla, r.TipoOperacion,
            r.Tabla, r.Columna ?? "", r.ValorAnterior ?? "", r.ValorNuevo ?? ""
        }));
}).RequireAuthorization();

app.MapGet("/retiros/{id:int}/pdf", async (int id, RetiroService retiros, IParametroRepository parametros) =>
{
    var retiro = await retiros.ObtenerAsync(id);
    if (retiro is null)
    {
        return Results.NotFound();
    }

    var nombreGaleria = await parametros.ObtenerAsync(Parametro.Claves.NombreGaleria) is { Length: > 0 } nombre
        ? nombre
        : "Galería ACATRAS";

    var pdf = RetiroPdfGenerator.Generar(retiro, nombreGaleria);
    return Results.File(pdf, "application/pdf", $"retiro-{id}.pdf");
}).RequireAuthorization();

app.Run();

// Encoder mínimo de CSV — no hace falta una librería para esto.
static class CsvHelper
{
    public static string Escapar(string valor) =>
        valor.Contains(';') || valor.Contains('"') || valor.Contains('\n')
            ? $"\"{valor.Replace("\"", "\"\"")}\""
            : valor;

    public static IResult Generar(string nombreArchivo, string[] encabezados, IEnumerable<string[]> filas)
    {
        var csv = new StringBuilder();
        csv.AppendLine(string.Join(';', encabezados));
        foreach (var fila in filas)
        {
            csv.AppendLine(string.Join(';', fila.Select(Escapar)));
        }

        // BOM UTF-8: sin esto, Excel abre las tildes rotas al abrir el CSV directamente.
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return Results.File(bytes, "text/csv", nombreArchivo);
    }
}

// Parseo de querystring para los endpoints de exportación — mismo criterio que
// SupplyParameterFromQuery usa en cada página, pero un Minimal API no tiene ese mecanismo.
static class QueryHelper
{
    public static int? Int(IQueryCollection q, string clave) => int.TryParse(q[clave], out var v) ? v : null;
    public static decimal? Decimal(IQueryCollection q, string clave) => decimal.TryParse(q[clave], out var v) ? v : null;
    public static DateOnly? Fecha(IQueryCollection q, string clave) => DateOnly.TryParse(q[clave], out var v) ? v : null;
    public static bool? Bool(IQueryCollection q, string clave) => bool.TryParse(q[clave], out var v) ? v : null;
    public static TEnum? Enum<TEnum>(IQueryCollection q, string clave) where TEnum : struct =>
        System.Enum.TryParse<TEnum>(q[clave], out var v) ? v : null;
    public static string Num(decimal valor) => valor.ToString(CultureInfo.InvariantCulture);
}
