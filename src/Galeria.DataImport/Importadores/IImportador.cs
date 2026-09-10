using Galeria.DataImport.Excel;

namespace Galeria.DataImport.Importadores;

/// <summary>
/// Un importador toma una porción de las planillas y la persiste. Se ejecutan en orden de
/// dependencia (parámetros → rubros → técnicas → artistas → obras → ventas → …): cada uno asume
/// que los mapas de <see cref="Contexto"/> ya tienen lo que cargaron los anteriores.
/// </summary>
public interface IImportador
{
    string Nombre { get; }

    Task EjecutarAsync(Fuentes fuentes, Contexto contexto, Informe informe);
}

/// <summary>Los libros de Excel de origen, por rol. Alguno puede faltar.</summary>
public sealed class Fuentes(LibroExcel principal, LibroExcel? control, LibroExcel? liquidaciones)
{
    /// <summary>"Acatràs sin los for next.xlsm": artistas, obras, movimientos, ventas, rubros, técnicas, parámetros.</summary>
    public LibroExcel Principal { get; } = principal;

    /// <summary>"Control.xlsx": auditoría histórica y cambios de precio.</summary>
    public LibroExcel? Control { get; } = control;

    /// <summary>"Liquidaciones.xlsm": pagos, adelantos, alquileres, retiros e historial de liquidaciones.</summary>
    public LibroExcel? Liquidaciones { get; } = liquidaciones;
}
