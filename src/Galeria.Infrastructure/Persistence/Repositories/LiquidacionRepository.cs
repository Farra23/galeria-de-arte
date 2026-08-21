using Galeria.Application.Liquidaciones;
using Galeria.Domain.Entities;
using Galeria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence.Repositories;

public class LiquidacionRepository(GaleriaDbContext db) : ILiquidacionRepository
{
    public async Task<List<LineaPendiente>> BuscarPendientesAsync(int artistaId, Moneda moneda, CancellationToken ct = default)
    {
        // Cualquier venta que ya haya salido en una liquidación anterior (como Venta, PagoContado
        // o Devolución) no se vuelve a ofrecer — mismo criterio para alquileres.
        var ventaIdsYaLiquidadas = await db.LineasLiquidacion
            .Where(l => (l.Tipo == TipoLinea.Venta || l.Tipo == TipoLinea.PagoContado || l.Tipo == TipoLinea.Devolucion)
                && l.ReferenciaId != null)
            .Select(l => l.ReferenciaId!.Value)
            .ToListAsync(ct);

        var alquilerIdsYaLiquidados = await db.LineasLiquidacion
            .Where(l => l.Tipo == TipoLinea.Alquiler && l.ReferenciaId != null)
            .Select(l => l.ReferenciaId!.Value)
            .ToListAsync(ct);

        var lineas = new List<LineaPendiente>();

        var ventas = await db.Ventas.AsNoTracking()
            .Include(v => v.Obra).ThenInclude(o => o.Artista)
            .Include(v => v.Devolucion)
            .Where(v => v.Obra.ArtistaId == artistaId && v.Moneda == moneda && !ventaIdsYaLiquidadas.Contains(v.Id))
            .ToListAsync(ct);

        foreach (var venta in ventas)
        {
            // Ya cobrada y devuelta: el requerimiento 8 la resuelve con un Adelanto automático
            // en el momento de la devolución — no vuelve a aparecer acá.
            if (venta.Devolucion is { ArtistaYaCobro: true })
            {
                continue;
            }

            if (venta.Devolucion is { ArtistaYaCobro: false })
            {
                lineas.Add(new LineaPendiente(TipoLinea.Devolucion, venta.Devolucion.Fecha, venta.ObraId,
                    CodigoObra(venta.Obra), venta.Obra.Titulo, venta.Cantidad, 0, 0, "Devuelta — no se cobra", venta.Id));
                continue;
            }

            if (venta.Obra.PagoContado)
            {
                lineas.Add(new LineaPendiente(TipoLinea.PagoContado, venta.Fecha, venta.ObraId,
                    CodigoObra(venta.Obra), venta.Obra.Titulo, venta.Cantidad, 0, 0, "Pago contado", venta.Id));
                continue;
            }

            // El costo es lo que eligió el artista al entregar la obra (requerimiento 10.2),
            // no el precio de venta final negociado con el cliente.
            var monto = venta.Obra.Costo * venta.Cantidad;
            lineas.Add(new LineaPendiente(TipoLinea.Venta, venta.Fecha, venta.ObraId,
                CodigoObra(venta.Obra), venta.Obra.Titulo, venta.Cantidad, venta.Obra.Costo, monto,
                venta.Obra.Observaciones, venta.Id));
        }

        var alquileres = await db.Alquileres.AsNoTracking()
            .Include(a => a.Obra).ThenInclude(o => o.Artista)
            .Where(a => a.Obra.ArtistaId == artistaId && a.Moneda == moneda && !alquilerIdsYaLiquidados.Contains(a.Id))
            .ToListAsync(ct);

        foreach (var alquiler in alquileres)
        {
            lineas.Add(new LineaPendiente(TipoLinea.Alquiler, alquiler.FechaInicio, alquiler.ObraId,
                CodigoObra(alquiler.Obra), alquiler.Obra.Titulo, 1, alquiler.MontoArtista, alquiler.MontoArtista,
                alquiler.Cliente is null ? "Alquiler" : $"Alquiler — {alquiler.Cliente}", alquiler.Id));
        }

        var adelantos = await db.Adelantos.AsNoTracking()
            .Where(a => a.ArtistaId == artistaId && a.Moneda == moneda && a.LiquidacionId == null)
            .ToListAsync(ct);

        foreach (var adelanto in adelantos)
        {
            lineas.Add(new LineaPendiente(TipoLinea.Adelanto, adelanto.Fecha, null, "",
                adelanto.Tipo == TipoAdelanto.Adelanto ? "Adelanto" : "Ajuste a favor", 1,
                adelanto.ImpactoEnLiquidacion, adelanto.ImpactoEnLiquidacion,
                adelanto.Observaciones, adelanto.Id));
        }

        return lineas.OrderBy(l => l.Fecha).ToList();
    }

    public async Task<DateOnly?> ObtenerUltimaFechaAsync(int artistaId, CancellationToken ct = default) =>
        await db.Liquidaciones.AsNoTracking()
            .Where(l => l.ArtistaId == artistaId)
            .OrderByDescending(l => l.Fecha)
            .Select(l => (DateOnly?)l.Fecha)
            .FirstOrDefaultAsync(ct);

    public async Task<int> ProximoNumeroAsync(CancellationToken ct = default)
    {
        var maximo = await db.Liquidaciones.MaxAsync(l => (int?)l.NumeroCorrelativo, ct);
        return (maximo ?? 0) + 1;
    }

    public Task AgregarAsync(Liquidacion liquidacion, CancellationToken ct = default)
    {
        db.Liquidaciones.Add(liquidacion);
        return Task.CompletedTask;
    }

    public async Task MarcarAdelantosDescontadosAsync(IEnumerable<int> adelantoIds, int liquidacionId, CancellationToken ct = default)
    {
        var ids = adelantoIds.ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var adelantos = await db.Adelantos.Where(a => ids.Contains(a.Id)).ToListAsync(ct);
        foreach (var adelanto in adelantos)
        {
            adelanto.LiquidacionId = liquidacionId;
        }
    }

    public async Task<List<LiquidacionListItem>> BuscarAsync(LiquidacionFiltro filtro, CancellationToken ct = default)
    {
        var query = db.Liquidaciones.AsNoTracking().Include(l => l.Artista).AsQueryable();

        if (filtro.ArtistaId is not null)
        {
            query = query.Where(l => l.ArtistaId == filtro.ArtistaId);
        }

        if (filtro.FechaDesde is not null)
        {
            query = query.Where(l => l.Fecha >= filtro.FechaDesde);
        }

        if (filtro.FechaHasta is not null)
        {
            query = query.Where(l => l.Fecha <= filtro.FechaHasta);
        }

        return await query
            .OrderByDescending(l => l.NumeroCorrelativo)
            .Select(l => new LiquidacionListItem(
                l.Id, l.NumeroCorrelativo, l.Fecha, l.Artista.Apellido + ", " + l.Artista.Nombre, l.Moneda, l.TotalNeto))
            .ToListAsync(ct);
    }

    public async Task<LiquidacionDetalle?> ObtenerDetalleAsync(int id, CancellationToken ct = default)
    {
        var liquidacion = await db.Liquidaciones.AsNoTracking()
            .Include(l => l.Artista)
            .Include(l => l.Lineas)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        if (liquidacion is null)
        {
            return null;
        }

        var lineas = liquidacion.Lineas
            .OrderBy(l => l.Fecha)
            .Select(l => new LineaPendiente(
                l.Tipo, l.Fecha, l.ObraId, l.CodigoObra, l.NombreObra, l.Cantidad, l.MontoUnitario, l.MontoTotal,
                l.Observaciones, l.ReferenciaId))
            .ToList();

        return new LiquidacionDetalle(
            liquidacion.Id,
            liquidacion.NumeroCorrelativo,
            liquidacion.Fecha,
            liquidacion.Artista.Apellido + ", " + liquidacion.Artista.Nombre,
            liquidacion.Moneda,
            lineas,
            liquidacion.TotalBruto,
            liquidacion.TotalAdelantos,
            liquidacion.TotalDevoluciones,
            liquidacion.TotalNeto);
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    private static string CodigoObra(Obra obra) => obra.Artista.Codigo.ToString("D3") + obra.NumeroObra.ToString("D3");
}
