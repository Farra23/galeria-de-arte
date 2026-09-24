using Galeria.Application.Artistas;
using Galeria.Application.Auditorias;
using Galeria.Application.Common;
using Galeria.Domain.Entities;
using Galeria.Domain.Enums;

namespace Galeria.Application.Liquidaciones;

// El motor de liquidación (requerimiento 10): junta todo lo pendiente de un artista en una
// moneda, calcula el total neto y, recién al confirmar, lo vuelca a un comprobante inmutable.
public class LiquidacionService(ILiquidacionRepository repositorio, IArtistaRepository artistas, AuditoriaService auditoria, IUnitOfWork unitOfWork)
{
    public Task<List<LiquidacionListItem>> BuscarAsync(LiquidacionFiltro filtro, CancellationToken ct = default) =>
        repositorio.BuscarAsync(filtro, ct);

    public Task<LiquidacionDetalle?> ObtenerDetalleAsync(int id, CancellationToken ct = default) =>
        repositorio.ObtenerDetalleAsync(id, ct);

    public Task<DateOnly?> ObtenerUltimaFechaAsync(int artistaId, CancellationToken ct = default) =>
        repositorio.ObtenerUltimaFechaAsync(artistaId, ct);

    public Task<Dictionary<int, DateOnly>> ObtenerUltimasFechasAsync(IReadOnlyCollection<int> artistaIds, CancellationToken ct = default) =>
        repositorio.ObtenerUltimasFechasAsync(artistaIds, ct);

    // La Agenda de pagos (requerimiento 11) necesita el mismo detalle línea por línea que la
    // vista previa de Liquidación, para partirlo entre "este mes" y "meses anteriores" (11.1).
    public Task<List<LineaPendiente>> ObtenerPendientesAsync(int artistaId, Moneda moneda, CancellationToken ct = default) =>
        repositorio.BuscarPendientesAsync(artistaId, moneda, ct);

    // Saldo actual a pagar en las dos monedas (requerimiento 4.2, encabezado de la ficha del
    // artista): misma "columna saldo a pagar" del requerimiento 4.1 que ya muestra la Lista de
    // Artistas — el total adeudado, sin el corte de FechaCorteLiquidable. Ese corte solo aplica
    // al generar/confirmar la liquidación en sí (item 5 del testeo del cliente): la plata sigue
    // debida igual, lo único que cambia es hasta qué fecha se la puede saldar hoy. Mostrar acá un
    // número más chico que en la Lista sería la inconsistencia real, no lo contrario.
    public async Task<(decimal Pesos, decimal Dolares)> ObtenerSaldoAsync(int artistaId, CancellationToken ct = default)
    {
        var pendientesPesos = await repositorio.BuscarPendientesAsync(artistaId, Moneda.Pesos, ct);
        var pendientesDolares = await repositorio.BuscarPendientesAsync(artistaId, Moneda.USD, ct);

        var (_, _, _, netoPesos) = CalcularTotales(pendientesPesos);
        var (_, _, _, netoDolares) = CalcularTotales(pendientesDolares);

        return (netoPesos, netoDolares);
    }

    // Item 5 del testeo del cliente: no se liquida lo vendido en el mes en curso, para no cerrar
    // un período que todavía puede cambiar (una devolución, una corrección de precio) antes de
    // que termine el mes. El corte es fijo -- hasta el último día del mes anterior -- y no editable
    // desde la pantalla, a propósito ("sin poder incluir lo vendido en el mes en curso").
    public static DateOnly FechaCorteLiquidable(DateOnly? hoy = null)
    {
        var fecha = hoy ?? DateOnly.FromDateTime(DateTime.Now);
        return new DateOnly(fecha.Year, fecha.Month, 1).AddDays(-1);
    }

    private static List<LineaPendiente> SoloLiquidables(List<LineaPendiente> lineas, DateOnly corte) =>
        lineas.Where(l => l.Fecha <= corte).ToList();

    // GRASP Information Expert + método puro y testable (sin acceso a datos): dado un conjunto de
    // líneas ya armado, calcula los totales. Separarlo de la consulta a la base es lo que permite
    // testear la aritmética de la liquidación sin levantar una base de datos (ver Galeria.Tests).
    public static (decimal TotalBruto, decimal TotalAdelantos, decimal TotalDevoluciones, decimal TotalNeto) CalcularTotales(
        IReadOnlyList<LineaPendiente> lineas)
    {
        var totalBruto = lineas
            .Where(l => l.Tipo is TipoLinea.Venta or TipoLinea.Alquiler or TipoLinea.PagoContado)
            .Sum(l => l.MontoTotal);

        // Ya vienen con signo (Adelanto.ImpactoEnLiquidacion): Adelanto resta, AjusteAFavor suma.
        var totalAdelantos = lineas.Where(l => l.Tipo == TipoLinea.Adelanto).Sum(l => l.MontoTotal);

        // Siempre 0 por construcción (requerimiento 10.2: "sin sumar plata") — se conserva como
        // total aparte para que la UI pueda mostrar cuántas devoluciones hubo sin que se pierdan
        // en el total bruto.
        var totalDevoluciones = lineas.Where(l => l.Tipo == TipoLinea.Devolucion).Sum(l => l.MontoTotal);

        var totalNeto = totalBruto + totalAdelantos + totalDevoluciones;

        return (totalBruto, totalAdelantos, totalDevoluciones, totalNeto);
    }

    public async Task<VistaPreviaLiquidacion> GenerarVistaPreviaAsync(int artistaId, Moneda moneda, CancellationToken ct = default)
    {
        var artista = await artistas.ObtenerFichaAsync(artistaId, ct)
            ?? throw new InvalidOperationException("El artista no existe.");

        var corte = FechaCorteLiquidable();
        var todasPendientes = await repositorio.BuscarPendientesAsync(artistaId, moneda, ct);
        var lineas = SoloLiquidables(todasPendientes, corte);
        var ultimaFecha = await repositorio.ObtenerUltimaFechaAsync(artistaId, ct);
        var (totalBruto, totalAdelantos, totalDevoluciones, totalNeto) = CalcularTotales(lineas);

        return new VistaPreviaLiquidacion(
            artistaId,
            artista.Apellido + ", " + artista.Nombre,
            moneda,
            ultimaFecha,
            lineas,
            totalBruto,
            totalAdelantos,
            totalDevoluciones,
            totalNeto,
            corte,
            todasPendientes.Count - lineas.Count,
            artista.Correo,
            artista.Celular);
    }

    // Irreversible a propósito (decisión del requerimiento 10.4, la más segura de las dos
    // opciones que planteaba el pedido original): una vez confirmada, la liquidación no se anula
    // desde acá. Auditoría deja el rastro completo igual.
    public Task<int> ConfirmarAsync(int artistaId, Moneda moneda, CancellationToken ct = default) =>
        unitOfWork.EjecutarEnTransaccionAsync(token => ConfirmarInternoAsync(artistaId, moneda, token), ct);

    // Dentro de una transacción: entre el alta de la liquidación y el marcado de los adelantos
    // como descontados no puede quedar un estado intermedio. Si quedaba, esos adelantos se
    // volvían a descontar en la liquidación siguiente — y una liquidación no se puede anular.
    private async Task<int> ConfirmarInternoAsync(int artistaId, Moneda moneda, CancellationToken ct)
    {
        var lineas = SoloLiquidables(await repositorio.BuscarPendientesAsync(artistaId, moneda, ct), FechaCorteLiquidable());

        if (lineas.Count == 0)
        {
            throw new InvalidOperationException("No hay nada pendiente para liquidar a este artista en esta moneda.");
        }

        var (totalBruto, totalAdelantos, totalDevoluciones, totalNeto) = CalcularTotales(lineas);
        var hoy = DateOnly.FromDateTime(DateTime.Now);

        var liquidacion = new Liquidacion
        {
            ArtistaId = artistaId,
            NumeroCorrelativo = await repositorio.ProximoNumeroAsync(ct),
            Fecha = hoy,
            PeriodoAnio = hoy.Year,
            PeriodoMes = hoy.Month,
            Moneda = moneda,
            Estado = EstadoLiquidacion.Confirmada,
            TotalBruto = totalBruto,
            TotalAdelantos = totalAdelantos,
            TotalDevoluciones = totalDevoluciones,
            TotalNeto = totalNeto,
            Lineas = lineas.Select(l => new LineaLiquidacion
            {
                Tipo = l.Tipo,
                ObraId = l.ObraId,
                Fecha = l.Fecha,
                CodigoObra = l.CodigoObra,
                NombreObra = l.NombreObra,
                Cantidad = l.Cantidad,
                MontoUnitario = l.MontoUnitario,
                MontoTotal = l.MontoTotal,
                Observaciones = l.Observaciones,
                ReferenciaId = l.ReferenciaId
            }).ToList()
        };

        await repositorio.AgregarAsync(liquidacion, ct);
        await repositorio.GuardarCambiosAsync(ct); // necesito liquidacion.Id antes de marcar los adelantos

        var adelantoIds = lineas
            .Where(l => l.Tipo == TipoLinea.Adelanto && l.ReferenciaId.HasValue)
            .Select(l => l.ReferenciaId!.Value);

        await repositorio.MarcarAdelantosDescontadosAsync(adelantoIds, liquidacion.Id, ct);

        await auditoria.RegistrarAsync(new RegistrarAuditoriaRequest(
            Pantalla: "Liquidaciones",
            TipoOperacion: "Alta",
            Tabla: "Liquidacion",
            ArtistaId: artistaId,
            EntidadId: liquidacion.Id.ToString(),
            ValorNuevo: $"Liquidación #{liquidacion.NumeroCorrelativo} — {totalNeto} {moneda}"), ct);

        await repositorio.GuardarCambiosAsync(ct);

        return liquidacion.Id;
    }
}
