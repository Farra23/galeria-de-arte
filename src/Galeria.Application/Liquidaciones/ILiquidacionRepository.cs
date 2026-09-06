using Galeria.Domain.Enums;

namespace Galeria.Application.Liquidaciones;

public interface ILiquidacionRepository
{
    // Todo lo que un artista tiene pendiente de cobrar en una moneda: ventas a costo, alquileres
    // (parte del artista), pago contado (a $0, solo para que quede constancia) y adelantos que
    // todavía no se descontaron. Nunca vuelve a ofrecer algo que ya está en otra liquidación.
    Task<List<LineaPendiente>> BuscarPendientesAsync(int artistaId, Moneda moneda, CancellationToken ct = default);

    Task<DateOnly?> ObtenerUltimaFechaAsync(int artistaId, CancellationToken ct = default);

    // Versión en lote de ObtenerUltimaFechaAsync -- una sola consulta agrupada en vez de una por
    // artista. La usan Resumen y Agenda, que necesitan esto para varios artistas a la vez (antes
    // era un round-trip a la base por cada uno con saldo pendiente).
    Task<Dictionary<int, DateOnly>> ObtenerUltimasFechasAsync(IReadOnlyCollection<int> artistaIds, CancellationToken ct = default);

    Task<int> ProximoNumeroAsync(CancellationToken ct = default);

    Task AgregarAsync(Domain.Entities.Liquidacion liquidacion, CancellationToken ct = default);

    // Los adelantos incluidos quedan "consumidos": no se ofrecen de nuevo en la próxima liquidación.
    Task MarcarAdelantosDescontadosAsync(IEnumerable<int> adelantoIds, int liquidacionId, CancellationToken ct = default);

    Task<List<LiquidacionListItem>> BuscarAsync(LiquidacionFiltro filtro, CancellationToken ct = default);

    Task<LiquidacionDetalle?> ObtenerDetalleAsync(int id, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
