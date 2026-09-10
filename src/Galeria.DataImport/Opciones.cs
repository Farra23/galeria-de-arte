namespace Galeria.DataImport;

/// <summary>
/// Parámetros de línea de comandos del importador.
/// Uso: <c>dotnet run --project src/Galeria.DataImport -- --origen datos-origen --salida src/Galeria.Web/Datos/app.db --recrear</c>
/// </summary>
public sealed record Opciones(string CarpetaOrigen, string ArchivoSalida, bool Recrear)
{
    public static Opciones Parsear(string[] args)
    {
        var origen = "datos-origen";
        var salida = Path.Combine("src", "Galeria.Web", "Datos", "app.db");
        var recrear = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--origen" when i + 1 < args.Length:
                    origen = args[++i];
                    break;
                case "--salida" when i + 1 < args.Length:
                    salida = args[++i];
                    break;
                case "--recrear":
                    recrear = true;
                    break;
                case "--help" or "-h":
                    ImprimirAyuda();
                    Environment.Exit(0);
                    break;
                default:
                    throw new ArgumentException($"Parámetro no reconocido: '{args[i]}'. Probá --help.");
            }
        }

        return new Opciones(origen, salida, recrear);
    }

    private static void ImprimirAyuda() => Console.WriteLine(
        """
        Importador de datos de las planillas Excel a la base del ERP.

          --origen  <carpeta>   Carpeta con los .xlsm/.xlsx de origen   (default: datos-origen)
          --salida  <archivo>   Archivo SQLite destino                  (default: src/Galeria.Web/Datos/app.db)
          --recrear             Borra la base y la crea desde cero antes de importar
          --help                Muestra esta ayuda

        Al terminar deja un informe en <carpeta origen>/informe-importacion.txt
        """);
}
