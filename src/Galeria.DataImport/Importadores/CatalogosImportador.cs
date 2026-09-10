using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Galeria.DataImport.Importadores;

/// <summary>
/// Parámetros del negocio, rubros y técnicas. Son las listas que sostienen el alta de obras, así
/// que van primero. Deduplica por clave normalizada (el Excel tiene técnicas repetidas con
/// distinta capitalización y espaciado: "Acrilico S/lienzo" vs "Acrilico s/ Lienzo").
/// </summary>
public sealed class CatalogosImportador : IImportador
{
    public string Nombre => "Catálogos (parámetros, rubros, técnicas)";

    private static readonly string[] NombresHojaTecnica = ["Técnica", "Tecnica", "Tècnica"];

    private static readonly HashSet<string> Encabezados =
        ["rubro", "rubros", "tecnica", "tecnicas", "perfil", "column1", "column2"];

    public async Task EjecutarAsync(Fuentes fuentes, Contexto contexto, Informe informe)
    {
        await ImportarParametrosAsync(fuentes, contexto, informe);
        await ImportarListaAsync(
            fuentes, contexto, informe, "Rubros", "Rubro", filasEncabezado: 2, columna: 2,
            contexto.Db.Rubros, nombre => new Rubro { Nombre = nombre }, maxLargo: 100);

        var hojaTecnica = NombresHojaTecnica.FirstOrDefault(fuentes.Principal.TieneHoja);
        if (hojaTecnica is null)
        {
            informe.Aviso("No se encontró la hoja de Técnicas en la planilla principal.");
            return;
        }

        await ImportarListaAsync(
            fuentes, contexto, informe, "Técnicas", hojaTecnica, filasEncabezado: 2, columna: 2,
            contexto.Db.Tecnicas, nombre => new Tecnica { Nombre = nombre }, maxLargo: 100);
    }

    private static async Task ImportarParametrosAsync(Fuentes fuentes, Contexto contexto, Informe informe)
    {
        var mapa = new Dictionary<string, string>
        {
            ["iva"] = Parametro.Claves.IvaPorcentaje,
            ["redondeo pesos"] = Parametro.Claves.RedondeosPesos,
            ["redondeo dolar"] = Parametro.Claves.RedondeoDolar,
        };

        var actualizados = 0;
        foreach (var fila in fuentes.Principal.Filas("Param", filasEncabezado: 0))
        {
            var clave = Texto.Clave(fila.Texto(1));
            var valor = fila.Decimal(2);
            if (!mapa.TryGetValue(clave, out var claveSistema) || valor is null)
            {
                continue;
            }

            var parametro = await contexto.Db.Parametros.FindAsync(claveSistema);
            var texto = valor.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (parametro is null)
            {
                contexto.Db.Parametros.Add(new Parametro { Clave = claveSistema, Valor = texto });
                actualizados++;
            }
            else if (parametro.Valor != texto)
            {
                parametro.Valor = texto;
                actualizados++;
            }
        }

        await contexto.Db.SaveChangesAsync();
        informe.Importados("Parámetros actualizados", actualizados);
    }

    private static async Task ImportarListaAsync<T>(
        Fuentes fuentes, Contexto contexto, Informe informe, string etiqueta, string hoja,
        int filasEncabezado, int columna, DbSet<T> conjunto, Func<string, T> crear, int maxLargo)
        where T : class
    {
        var propiedadNombre = typeof(T).GetProperty("Nombre")!;
        var vistas = new HashSet<string>();
        foreach (var existente in await conjunto.ToListAsync())
        {
            vistas.Add(Texto.Clave((string)propiedadNombre.GetValue(existente)!));
        }

        var nuevos = 0;
        foreach (var fila in fuentes.Principal.Filas(hoja, filasEncabezado))
        {
            var crudo = Texto.Limpiar(fila.Texto(columna));
            if (crudo is null || Encabezados.Contains(Texto.Clave(crudo)))
            {
                continue;
            }

            if (!vistas.Add(Texto.Clave(crudo)))
            {
                continue;
            }

            var prolijo = Texto.TituloProlijo(crudo);
            if (prolijo.Length > maxLargo)
            {
                prolijo = prolijo[..maxLargo];
            }

            conjunto.Add(crear(prolijo));
            nuevos++;
        }

        await contexto.Db.SaveChangesAsync();
        informe.Importados(etiqueta, nuevos);
    }
}
