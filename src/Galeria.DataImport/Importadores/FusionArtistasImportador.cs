using Galeria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Galeria.DataImport.Importadores;

/// <summary>
/// Unifica artistas que estaban cargados con dos códigos (la misma persona, historia partida).
/// Lee <c>datos-origen/artistas-fusionar.csv</c> — una línea por par: <c>absorbido ; canónico</c>.
///
/// Mueve TODO del código absorbido al canónico: obras, ventas (van pegadas a la obra),
/// liquidaciones, adelantos, series, auditoría y agenda. Si una obra del absorbido choca de número
/// con una del canónico, se le da un número nuevo libre (hay que reimprimir esa etiqueta). Al
/// final el código absorbido queda inactivo (no aparece, pero no se borra).
///
/// Corre DESPUÉS de importar obras/ventas/adelantos/precios y ANTES de las liquidaciones de
/// apertura — así la apertura ve un solo artista con toda su historia junta.
/// </summary>
public sealed class FusionArtistasImportador : IImportador
{
    public string Nombre => "Fusión de artistas duplicados";

    public async Task EjecutarAsync(Fuentes fuentes, Contexto contexto, Informe informe)
    {
        var ruta = Path.Combine(fuentes.CarpetaOrigen, "artistas-fusionar.csv");
        if (!File.Exists(ruta))
        {
            return;
        }

        var fusiones = 0;
        var obrasRecodificadas = 0;
        var etiquetasAReimprimir = new List<string>();

        foreach (var linea in File.ReadAllLines(ruta))
        {
            var texto = linea.Split('#')[0].Trim();
            if (texto.Length == 0)
            {
                continue;
            }

            var partes = texto.Split([';', ','], StringSplitOptions.TrimEntries);
            if (partes.Length < 2 || !int.TryParse(partes[0], out var codAbs) || !int.TryParse(partes[1], out var codCan))
            {
                informe.Rechazo(Nombre, $"línea ilegible: {texto}");
                continue;
            }

            var absorbido = await contexto.Db.Artistas.FirstOrDefaultAsync(a => a.Codigo == codAbs);
            var canonico = await contexto.Db.Artistas.FirstOrDefaultAsync(a => a.Codigo == codCan);
            if (absorbido is null || canonico is null)
            {
                informe.Rechazo(Nombre, $"código {(absorbido is null ? codAbs : codCan)} no existe");
                continue;
            }

            // 1) Contacto: lo que le falte al canónico, se lo pasa el absorbido.
            canonico.Celular ??= absorbido.Celular;
            canonico.TelFijo ??= absorbido.TelFijo;
            canonico.Correo ??= absorbido.Correo;
            canonico.Taller ??= absorbido.Taller;
            canonico.Direccion ??= absorbido.Direccion;
            canonico.Perfil ??= absorbido.Perfil;

            // 2) Obras: mover al canónico, resolviendo choques de número.
            var numerosCanonico = await contexto.Db.Obras
                .Where(o => o.ArtistaId == canonico.Id).Select(o => o.NumeroObra).ToListAsync();
            var usados = new HashSet<int>(numerosCanonico);
            var proximo = (usados.Count > 0 ? usados.Max() : 0) + 1;

            var obrasAbs = await contexto.Db.Obras
                .Where(o => o.ArtistaId == absorbido.Id).OrderBy(o => o.NumeroObra).ToListAsync();

            foreach (var obra in obrasAbs)
            {
                var numeroFinal = obra.NumeroObra;
                if (!usados.Add(numeroFinal))
                {
                    numeroFinal = proximo++;
                    obrasRecodificadas++;
                    obra.Observaciones = Recortar(
                        $"Código reasignado al unificar con el artista {codCan:D3} (era {codAbs:D3}{obra.NumeroObra:D3}). {obra.Observaciones}".Trim(), 2000);

                    // Solo importa reimprimir la etiqueta de las que tienen stock (las vendidas
                    // son historia).
                    if (obra.Existencia > 0)
                    {
                        etiquetasAReimprimir.Add(
                            $"{codAbs:D3}{obra.NumeroObra:D3} → {codCan:D3}{numeroFinal:D3}  «{obra.Titulo}» (stock {obra.Existencia})");
                    }
                }

                obra.NumeroObra = numeroFinal;
                obra.ArtistaId = canonico.Id;

                // Código desnormalizado en las líneas de liquidación de esa obra.
                var nuevoCodigo = $"{codCan:D3}{numeroFinal:D3}";
                await contexto.Db.LineasLiquidacion
                    .Where(l => l.ObraId == obra.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(l => l.CodigoObra, nuevoCodigo));
            }

            // 3) El resto de las tablas que apuntan al artista por Id.
            await contexto.Db.Liquidaciones.Where(x => x.ArtistaId == absorbido.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ArtistaId, canonico.Id));
            await contexto.Db.Adelantos.Where(x => x.ArtistaId == absorbido.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ArtistaId, canonico.Id));
            await contexto.Db.Series.Where(x => x.ArtistaId == absorbido.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ArtistaId, canonico.Id));
            await contexto.Db.AgendasPago.Where(x => x.ArtistaId == absorbido.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ArtistaId, canonico.Id));
            await contexto.Db.Auditorias.Where(x => x.ArtistaId == absorbido.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ArtistaId, (int?)canonico.Id));

            // 4) El absorbido queda inactivo, y su código sigue resolviendo al canónico.
            absorbido.Activo = false;
            await contexto.Db.SaveChangesAsync();
            contexto.Db.ChangeTracker.Clear();
            contexto.ArtistaPorCodigo[codAbs] = canonico.Id;

            fusiones++;
            informe.Aviso($"Unificado: código {codAbs} → {codCan} ({canonico.NombreCompleto}). " +
                          $"Todo (obras, ventas, liquidaciones, adelantos) quedó en la ficha del {codCan}.");
        }

        // Una AgendaPago por artista es única — si quedaron dos (absorbido + canónico), borrar la vieja.
        await LimpiarAgendasDuplicadasAsync(contexto);

        informe.Importados(Nombre, fusiones);
        if (obrasRecodificadas > 0)
        {
            informe.Aviso($"Al unificar, {obrasRecodificadas} obras del código absorbido recibieron un código nuevo " +
                          $"(chocaban de número con el canónico). De esas, {etiquetasAReimprimir.Count} tienen stock y " +
                          "necesitan etiqueta nueva:");
            foreach (var e in etiquetasAReimprimir)
            {
                informe.Aviso($"   {e}");
            }
        }
    }

    private static async Task LimpiarAgendasDuplicadasAsync(Contexto contexto)
    {
        // Una AgendaPago por artista (índice único). Si una fusión dejó dos, se conserva la más
        // vieja y se borran las demás.
        var todas = await contexto.Db.AgendasPago
            .Select(a => new { a.Id, a.ArtistaId }).ToListAsync();

        var sobrantes = todas
            .GroupBy(a => a.ArtistaId)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.OrderBy(a => a.Id).Skip(1).Select(a => a.Id))
            .ToList();

        if (sobrantes.Count > 0)
        {
            await contexto.Db.AgendasPago.Where(a => sobrantes.Contains(a.Id)).ExecuteDeleteAsync();
        }
    }

    private static string? Recortar(string? valor, int max) =>
        valor is not null && valor.Length > max ? valor[..max] : valor;
}
