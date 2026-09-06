using Galeria.Application.Alquileres;
using Galeria.Application.Artistas;
using Galeria.Application.Liquidaciones;
using Galeria.Application.Obras;
using Galeria.Application.Retiros;
using Galeria.Application.Ventas;
using Galeria.Domain.Enums;

namespace Galeria.Application.Resumen;

// Dashboard (requerimiento 2): a propósito NO tiene repositorio propio. Es un servicio agregador
// (Facade) que compone los servicios que ya existen para cada módulo — nada de lo que necesita la
// pantalla de Resumen es información nueva, es una vista distinta de datos que Ventas, Artistas,
// Obras, Retiros y Alquileres ya exponen.
public class ResumenService(
    ArtistaService artistas,
    ObraService obras,
    VentaService ventas,
    RetiroService retiros,
    AlquilerService alquileres,
    LiquidacionService liquidaciones)
{
    public async Task<ResumenDashboard> ObtenerAsync(CancellationToken ct = default)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var primerDiaDelMes = new DateOnly(hoy.Year, hoy.Month, 1);

        var ventasDelMesList = await ventas.BuscarAsync(new VentaFiltro(FechaDesde: primerDiaDelMes), ct);
        var ventasDelMes = new VentaDelMesResumen(
            ventasDelMesList.Count,
            ventasDelMesList.Where(v => v.Moneda == Moneda.Pesos).Sum(v => v.PrecioVenta),
            ventasDelMesList.Where(v => v.Moneda == Moneda.USD).Sum(v => v.PrecioVenta));

        var ultimasVentas = await ventas.ObtenerUltimasAsync(5, ct);

        var todosLosArtistas = await artistas.BuscarAsync(new ArtistaFiltro(), ct);
        var conDeuda = todosLosArtistas.Where(a => a.SaldoPesos > 0 || a.SaldoDolares > 0).ToList();

        // La señal de "a este llamalo ya" (0.1 "+"): hace cuánto que no cobra, los más viejos
        // arriba — un artista que nunca cobró (null) es el caso más urgente, va primero.
        // Una sola consulta agrupada para todos en vez de una por artista (antes era un
        // round-trip a la base por cada artista con deuda, en cada carga del Resumen).
        var ultimasFechas = await liquidaciones.ObtenerUltimasFechasAsync(conDeuda.Select(a => a.Id).ToList(), ct);
        var artistasConDeuda = conDeuda
            .Select(artista => new ArtistaConDeuda(
                artista.Id,
                artista.NombreCompleto,
                artista.SaldoPesos,
                artista.SaldoDolares,
                ultimasFechas.TryGetValue(artista.Id, out var fecha) ? fecha : null))
            .ToList();

        artistasConDeuda = artistasConDeuda
            .OrderBy(a => a.UltimaLiquidacion.HasValue)
            .ThenBy(a => a.UltimaLiquidacion)
            .ToList();
        var totalArtistasConDeuda = artistasConDeuda.Count;
        artistasConDeuda = artistasConDeuda.Take(5).ToList();

        var todasLasObras = await obras.BuscarAsync(new ObraFiltro(), ct);
        var todosLosRetiros = await retiros.BuscarAsync(new RetiroFiltro(), ct);
        var todosLosAlquileres = await alquileres.BuscarAsync(new AlquilerFiltro(), ct);

        var avisos = new AvisosOperativos(
            todasLasObras.Count(o => o.Tecnica is null),
            todasLasObras.Count(o => o.Rubro is null),
            todosLosRetiros.Count(r => r.EstaVencido),
            todosLosAlquileres.Count(a => a.EstaActivo && a.FechaRecupero is null));

        return new ResumenDashboard(
            ventasDelMes,
            ultimasVentas,
            conDeuda.Where(a => a.SaldoPesos > 0).Sum(a => a.SaldoPesos),
            conDeuda.Where(a => a.SaldoDolares > 0).Sum(a => a.SaldoDolares),
            artistasConDeuda,
            totalArtistasConDeuda,
            avisos);
    }
}
