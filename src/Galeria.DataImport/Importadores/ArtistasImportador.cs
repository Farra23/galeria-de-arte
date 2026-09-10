using Galeria.Domain.Entities;

namespace Galeria.DataImport.Importadores;

/// <summary>
/// Hoja "Artistas" de la planilla principal. El maestro tiene ~264 filas, varias son ranuras
/// vacías (solo un código) y hay que descartarlas. El código de artista se respeta tal cual
/// (decisión: hay huecos históricos del 102 al 990, no es un autonumérico).
/// </summary>
public sealed class ArtistasImportador : IImportador
{
    public string Nombre => "Artistas";

    private const int ColCodigo = 1, ColApellido = 2, ColNombre = 3, ColPerfil = 4,
        ColTaller = 5, ColCelular = 6, ColTelFijo = 7, ColDireccion = 9, ColCorreo = 10;

    public async Task EjecutarAsync(Fuentes fuentes, Contexto contexto, Informe informe)
    {
        var codigosVistos = new HashSet<int>();
        var importados = 0;

        foreach (var fila in fuentes.Principal.Filas("Artistas", filasEncabezado: 1))
        {
            var codigo = fila.Entero(ColCodigo);
            var apellido = Recortar(fila.Texto(ColApellido), 100);
            var nombre = Recortar(fila.Texto(ColNombre), 100);

            if (codigo is null)
            {
                informe.Rechazo(Nombre, "fila sin código de artista");
                continue;
            }

            if (apellido is null && nombre is null)
            {
                informe.Rechazo(Nombre, "ranura de código sin nombre (fila vacía del maestro)");
                continue;
            }

            if (!codigosVistos.Add(codigo.Value))
            {
                informe.Rechazo(Nombre, $"código de artista duplicado ({codigo})");
                continue;
            }

            contexto.Db.Artistas.Add(new Artista
            {
                Codigo = codigo.Value,
                Apellido = apellido ?? nombre!,
                Nombre = apellido is null ? string.Empty : nombre ?? string.Empty,
                Perfil = Recortar(fila.Texto(ColPerfil), 2000),
                Taller = Recortar(fila.Texto(ColTaller), 200),
                Celular = Recortar(fila.Texto(ColCelular), 30),
                TelFijo = Recortar(fila.Texto(ColTelFijo), 30),
                Direccion = Recortar(fila.Texto(ColDireccion), 300),
                Correo = Recortar(fila.Texto(ColCorreo), 200),
                Activo = true,
            });
            importados++;
        }

        await contexto.Db.SaveChangesAsync();
        await contexto.RecargarMapasAsync();
        informe.Importados(Nombre, importados);
    }

    private static string? Recortar(string? valor, int max)
    {
        var limpio = Texto.Limpiar(valor);
        return limpio is not null && limpio.Length > max ? limpio[..max] : limpio;
    }
}
