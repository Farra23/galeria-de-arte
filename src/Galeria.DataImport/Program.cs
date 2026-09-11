using Galeria.DataImport;
using Galeria.DataImport.Excel;
using Galeria.DataImport.Importadores;
using Galeria.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var opciones = Opciones.Parsear(args);

Console.WriteLine("Importador de datos — ERP Galería ACATRAS");
Console.WriteLine($"  Origen : {Path.GetFullPath(opciones.CarpetaOrigen)}");
Console.WriteLine($"  Salida : {Path.GetFullPath(opciones.ArchivoSalida)}");
Console.WriteLine($"  Modo   : {(opciones.Recrear ? "recrear base desde cero" : "agregar sobre la base existente")}");
Console.WriteLine();

var archivoPrincipal = ResolverArchivo(opciones.CarpetaOrigen, "Acatràs sin los for next.xlsm", "Acatras sin los for next.xlsm");
if (archivoPrincipal is null)
{
    Console.Error.WriteLine($"No se encontró la planilla principal en {opciones.CarpetaOrigen}.");
    return 1;
}

if (opciones.Recrear && File.Exists(opciones.ArchivoSalida))
{
    foreach (var sufijo in new[] { "", "-wal", "-shm" })
    {
        var f = opciones.ArchivoSalida + sufijo;
        if (File.Exists(f))
        {
            File.Delete(f);
        }
    }

    Console.WriteLine("Base anterior borrada.");
}

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(opciones.ArchivoSalida))!);

var conexion = $"DataSource={opciones.ArchivoSalida}";
var dbOptions = new DbContextOptionsBuilder<GaleriaDbContext>()
    .UseSqlite(conexion)
    .Options;

await using var db = new GaleriaDbContext(dbOptions);

Console.WriteLine("Aplicando migraciones...");
await db.Database.MigrateAsync();

var informe = new Informe();
var contexto = new Contexto(db);
await contexto.RecargarMapasAsync();

using var principal = new LibroExcel(archivoPrincipal);
using var control = AbrirOpcional(opciones.CarpetaOrigen, "Control.xlsx");
using var liquidaciones = AbrirOpcional(opciones.CarpetaOrigen, "Liquidaciones.xlsm");
var fuentes = new Fuentes(principal, control, liquidaciones, opciones.CarpetaOrigen);

IImportador[] importadores =
[
    new CatalogosImportador(),
    new ArtistasImportador(),
    new ObrasImportador(),
    new VentasImportador(),
    new AdelantosImportador(),
    new PreciosImportador(),
    new FusionArtistasImportador(),
    new AperturaImportador(),
];

foreach (var importador in importadores)
{
    Console.WriteLine($"→ {importador.Nombre}...");
    var reloj = System.Diagnostics.Stopwatch.StartNew();
    await importador.EjecutarAsync(fuentes, contexto, informe);
    Console.WriteLine($"   ({reloj.Elapsed.TotalSeconds:F1}s)");
}

var rutaInforme = Path.Combine(opciones.CarpetaOrigen, "informe-importacion.txt");
informe.Escribir(rutaInforme);
informe.ImprimirResumen();
Console.WriteLine();
Console.WriteLine($"Informe detallado: {Path.GetFullPath(rutaInforme)}");
return 0;

static string? ResolverArchivo(string carpeta, params string[] nombres)
{
    foreach (var nombre in nombres)
    {
        var ruta = Path.Combine(carpeta, nombre);
        if (File.Exists(ruta))
        {
            return ruta;
        }
    }

    // Último recurso: cualquier .xlsm que empiece con "Acatr"
    return Directory.EnumerateFiles(carpeta, "Acatr*.xls?").FirstOrDefault();
}

static LibroExcel? AbrirOpcional(string carpeta, string nombre)
{
    var ruta = Path.Combine(carpeta, nombre);
    return File.Exists(ruta) ? new LibroExcel(ruta) : null;
}
