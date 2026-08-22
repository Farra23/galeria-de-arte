using Galeria.Application.Artistas;
using Galeria.Application.Liquidaciones;
using Galeria.Domain.Entities;
using Galeria.Domain.Enums;

namespace Galeria.Application.Agenda;

// Facade igual que ResumenService: compone Artistas y Liquidaciones en vez de tener su propia
// consulta de saldo. "Se genera automáticamente" (requerimiento 11) — la única parte que persiste
// acá es lo que alguien escribe a mano (fecha pactada, confirmada, comentarios).
public class AgendaService(ArtistaService artistas, LiquidacionService liquidaciones, IAgendaPagoRepository agendaRepo)
{
    public async Task<List<AgendaFilaItem>> ObtenerAsync(CancellationToken ct = default)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);

        var todosLosArtistas = await artistas.BuscarAsync(new ArtistaFiltro(), ct);
        var conSaldo = todosLosArtistas.Where(a => a.SaldoPesos > 0 || a.SaldoDolares > 0).ToList();
        var seguimientos = await agendaRepo.ObtenerTodasAsync(ct);

        var filas = new List<AgendaFilaItem>();

        foreach (var artista in conSaldo)
        {
            var pendientesPesos = await liquidaciones.ObtenerPendientesAsync(artista.Id, Moneda.Pesos, ct);
            var pendientesDolares = await liquidaciones.ObtenerPendientesAsync(artista.Id, Moneda.USD, ct);

            var piezasDelMes = pendientesPesos.Concat(pendientesDolares)
                .Count(l => l.Tipo is TipoLinea.Venta or TipoLinea.Alquiler or TipoLinea.Adelanto && EsDelMes(l.Fecha, hoy));

            var (pesosDelMes, pesosAnteriores) = Partir(pendientesPesos, hoy);
            var (dolaresDelMes, dolaresAnteriores) = Partir(pendientesDolares, hoy);

            seguimientos.TryGetValue(artista.Id, out var seguimiento);

            filas.Add(new AgendaFilaItem(
                artista.Id,
                artista.NombreCompleto,
                piezasDelMes,
                pesosDelMes,
                dolaresDelMes,
                pesosAnteriores,
                dolaresAnteriores,
                pesosDelMes + pesosAnteriores,
                dolaresDelMes + dolaresAnteriores,
                seguimiento?.Comentarios,
                seguimiento?.FechaPactada,
                seguimiento?.FechaConfirmada,
                artista.Correo,
                artista.Celular));
        }

        return filas;
    }

    public Task GuardarAsync(GuardarAgendaRequest request, CancellationToken ct = default) =>
        agendaRepo.GuardarAsync(request, ct);

    // Regla del arrastre (requerimiento 11.1): solo si no se liquidó se suma a "plata meses
    // anteriores" — acá ya está garantizado, porque BuscarPendientesAsync nunca devuelve algo que
    // ya esté en una liquidación confirmada.
    private static (decimal DelMes, decimal Anteriores) Partir(List<LineaPendiente> lineas, DateOnly hoy)
    {
        var delMes = lineas.Where(l => EsDelMes(l.Fecha, hoy)).ToList();
        var anteriores = lineas.Where(l => !EsDelMes(l.Fecha, hoy)).ToList();

        var (_, _, _, netoDelMes) = LiquidacionService.CalcularTotales(delMes);
        var (_, _, _, netoAnteriores) = LiquidacionService.CalcularTotales(anteriores);

        return (netoDelMes, netoAnteriores);
    }

    private static bool EsDelMes(DateOnly fecha, DateOnly hoy) => fecha.Year == hoy.Year && fecha.Month == hoy.Month;
}
