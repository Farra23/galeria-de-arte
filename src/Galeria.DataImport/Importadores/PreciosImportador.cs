using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Galeria.DataImport.Importadores;

/// <summary>
/// Hoja "CambioPrecios" de Control.xlsx (~1.300 filas): el historial de cambios de precio de las
/// obras. Se importa como registros de auditoría con Columna = "PrecioVenta", que es exactamente
/// lo que consulta la pestaña "Historial de precios" de la ficha de la obra.
/// </summary>
public sealed class PreciosImportador : IImportador
{
    public string Nombre => "Historial de precios";

    private const int ColCodigo = 1, ColFecha = 2, ColPrecioAnterior = 4, ColPrecioActual = 5;

    public async Task EjecutarAsync(Fuentes fuentes, Contexto contexto, Informe informe)
    {
        if (fuentes.Control is null)
        {
            informe.Aviso("No se encontró Control.xlsx — no se importó el historial de precios.");
            return;
        }

        var artistaDeObra = await contexto.Db.Obras
            .Select(o => new { o.Id, o.ArtistaId })
            .ToDictionaryAsync(x => x.Id, x => x.ArtistaId);

        var pendientes = new List<Auditoria>(2000);
        var importados = 0;

        foreach (var fila in fuentes.Control.Filas("CambioPrecios", filasEncabezado: 2))
        {
            var codigo = fila.Texto(ColCodigo);
            if (codigo is null || codigo.Length < 4 || !codigo.All(char.IsDigit))
            {
                informe.Rechazo(Nombre, "código de obra ilegible");
                continue;
            }

            var clave = (int.Parse(codigo[..3]), int.Parse(codigo[3..]));
            if (!contexto.ObraPorCodigo.TryGetValue(clave, out var obraId))
            {
                informe.Rechazo(Nombre, "obra del cambio de precio ya no está en el catálogo");
                continue;
            }

            var fecha = fila.Fecha(ColFecha);
            if (fecha is null || fecha.Value.Year is < 2000 or > 2030)
            {
                informe.Rechazo(Nombre, "cambio de precio con fecha inválida");
                continue;
            }

            var anterior = fila.Decimal(ColPrecioAnterior);
            var actual = fila.Decimal(ColPrecioActual);
            if (actual is null)
            {
                continue;
            }

            pendientes.Add(new Auditoria
            {
                Timestamp = new DateTimeOffset(fecha.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                UsuarioId = "migracion",
                NombreUsuario = "Migración del Excel",
                Pantalla = "Obras",
                TipoOperacion = "Modificacion",
                Tabla = "Obra",
                Columna = "PrecioVenta",
                ValorAnterior = anterior?.ToString("0.##"),
                ValorNuevo = actual.Value.ToString("0.##"),
                ObraId = obraId,
                ArtistaId = artistaDeObra.GetValueOrDefault(obraId),
                EntidadId = obraId.ToString(),
            });
            importados++;

            if (pendientes.Count >= 2000)
            {
                await GuardarAsync(contexto, pendientes);
            }
        }

        await GuardarAsync(contexto, pendientes);
        informe.Importados(Nombre, importados);
    }

    private static async Task GuardarAsync(Contexto contexto, List<Auditoria> pendientes)
    {
        if (pendientes.Count == 0)
        {
            return;
        }

        contexto.Db.Auditorias.AddRange(pendientes);
        await contexto.Db.SaveChangesAsync();
        contexto.Db.ChangeTracker.Clear();
        pendientes.Clear();
    }
}
