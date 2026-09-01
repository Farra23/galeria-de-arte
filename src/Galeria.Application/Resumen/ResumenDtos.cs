namespace Galeria.Application.Resumen;

public record VentaDelMesResumen(int Cantidad, decimal TotalPesos, decimal TotalDolares);

public record ArtistaConDeuda(int ArtistaId, string ArtistaNombre, decimal SaldoPesos, decimal SaldoDolares, DateOnly? UltimaLiquidacion);

public record AvisosOperativos(int ObrasSinTecnica, int ObrasSinRubro, int RetirosVencidos, int AlquileresSinFechaRecupero);

public record ResumenDashboard(
    VentaDelMesResumen VentasDelMes,
    List<Ventas.VentaListItem> UltimasVentas,
    decimal TotalAdeudadoPesos,
    decimal TotalAdeudadoDolares,
    // Recorte para que el Resumen no dependa del tamaño de la base (podría haber cientos de
    // artistas con deuda) — TotalArtistasConDeuda es el conteo real para el aviso de Pendientes,
    // ArtistasConDeuda es solo lo que se muestra en la tabla (los más urgentes primero).
    List<ArtistaConDeuda> ArtistasConDeuda,
    int TotalArtistasConDeuda,
    AvisosOperativos Avisos);
