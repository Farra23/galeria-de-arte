using Galeria.Application.Artistas;
using Galeria.Application.Auditorias;
using Galeria.Domain.Entities;
using Galeria.Domain.Enums;

namespace Galeria.Application.Liquidaciones;

// El motor de liquidación (requerimiento 10): junta todo lo pendiente de un artista en una
// moneda, calcula el total neto y, recién al confirmar, lo vuelca a un comprobante inmutable.
public class LiquidacionService(ILiquidacionRepository repositorio, IArtistaRepository artistas, AuditoriaService auditoria)
{
    public Task<List<LiquidacionListItem>> BuscarAsync(LiquidacionFiltro filtro, CancellationToken ct = default) =>
        repositorio.BuscarAsync(filtro, ct);

    public Task<LiquidacionDetalle?> ObtenerDetalleAsync(int id, CancellationToken ct = default) =>
        repositorio.ObtenerDetalleAsync(id, ct);

    // Saldo actual a pagar en las dos monedas (requerimiento 4.2, encabezado de la ficha del
    // artista): misma fuente que usa Generar liquidación, así el número de la ficha nunca queda
    // desincronizado de lo que efectivamente se liquidaría si se confirma ahora.
    public async Task<(decimal Pesos, decimal Dolares)> ObtenerSaldoAsync(int artistaId, CancellationToken ct = default)
    {
        var pendientesPesos = await repositorio.BuscarPendientesAsync(artistaId, Moneda.Pesos, ct);
        var pendientesDolares = await repositorio.BuscarPendientesAsync(artistaId, Moneda.USD, ct);

        var (_, _, _, netoPesos) = CalcularTotales(pendientesPesos);
        var (_, _, _, netoDolares) = CalcularTotales(pendientesDolares);

        return (netoPesos, netoDolares);
    }

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

        var lineas = await repositorio.BuscarPendientesAsync(artistaId, moneda, ct);
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
            totalNeto);
    }

    // Irreversible a propósito (decisión del requerimiento 10.4, la más segura de las dos
    // opciones que planteaba el pedido original): una vez confirmada, la liquidación no se anula
    // desde acá. Auditoría deja el rastro completo igual.
    public async Task<int> ConfirmarAsync(int artistaId, Moneda moneda, CancellationToken ct = default)
    {
        var lineas = await repositorio.BuscarPendientesAsync(artistaId, moneda, ct);

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
