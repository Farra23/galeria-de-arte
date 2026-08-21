namespace Galeria.Application.Resumen;

public record VentaDelMesResumen(int Cantidad, decimal TotalPesos, decimal TotalDolares);

public record ArtistaConDeuda(int ArtistaId, string ArtistaNombre, decimal SaldoPesos, decimal SaldoDolares, DateOnly? UltimaLiquidacion);

public record AvisosOperativos(int ObrasSinTecnica, int RetirosVencidos, int AlquileresSinFechaRecupero);

public record ResumenDashboard(
    VentaDelMesResumen VentasDelMes,
    List<Ventas.VentaListItem> UltimasVentas,
    decimal TotalAdeudadoPesos,
    decimal TotalAdeudadoDolares,
    List<ArtistaConDeuda> ArtistasConDeuda,
    AvisosOperativos Avisos);
