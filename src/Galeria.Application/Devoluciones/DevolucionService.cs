using Galeria.Application.Adelantos;
using Galeria.Application.Auditorias;
using Galeria.Application.Obras;
using Galeria.Application.Ventas;
using Galeria.Domain.Entities;
using Galeria.Domain.Enums;

namespace Galeria.Application.Devoluciones;

public class DevolucionService(
    IDevolucionRepository devoluciones,
    IVentaRepository ventas,
    IObraRepository obras,
    AdelantoService adelantoService,
    AuditoriaService auditoria)
{
    public Task<List<VentaParaDevolucion>> BuscarVentasAsync(string? texto, CancellationToken ct = default) =>
        devoluciones.BuscarVentasAsync(texto, ct);

    public Task<List<DevolucionListItem>> BuscarAsync(DevolucionFiltro filtro, CancellationToken ct = default) =>
        devoluciones.BuscarAsync(filtro, ct);

    // El punto delicado del requerimiento 8: la pieza siempre vuelve al stock, pero la plata se
    // trata distinto según si el artista ya cobró esa venta.
    // - Ya cobró → se genera un Adelanto negativo automático (a descontar de la próxima liquidación).
    // - Todavía no cobró → la línea "Devolución" aparece sin sumar plata en la próxima liquidación
    //   (eso lo resuelve el motor de Liquidaciones excluyendo la venta devuelta).
    public async Task<int> RegistrarAsync(RegistrarDevolucionRequest request, CancellationToken ct = default)
    {
        var venta = await ventas.ObtenerEntidadAsync(request.VentaId, ct)
            ?? throw new InvalidOperationException("La venta no existe.");

        var obra = await obras.ObtenerEntidadAsync(venta.ObraId, ct)
            ?? throw new InvalidOperationException("La obra ya no existe.");

        var devolucion = new Devolucion
        {
            VentaId = venta.Id,
            Fecha = request.Fecha,
            Motivo = request.Motivo,
            ArtistaYaCobro = request.ArtistaYaCobro
        };

        await devoluciones.AgregarAsync(devolucion, ct);
        await devoluciones.GuardarCambiosAsync(ct);

        obra.Existencia += venta.Cantidad;
        obra.Estado = EstadoObra.Disponible;

        await obras.RegistrarMovimientoAsync(new Movimiento
        {
            ObraId = obra.Id,
            Fecha = request.Fecha,
            Tipo = TipoMovimiento.Devolucion,
            Cantidad = venta.Cantidad,
            Moneda = obra.Moneda,
            ReferenciaId = devolucion.Id
        }, ct);

        await auditoria.RegistrarAsync(new RegistrarAuditoriaRequest(
            Pantalla: "Devoluciones",
            TipoOperacion: "Alta",
            Tabla: "Devolucion",
            ArtistaId: obra.ArtistaId,
            ObraId: obra.Id,
            EntidadId: devolucion.Id.ToString(),
            ValorNuevo: $"Devolución de venta #{venta.Id} — artista ya cobró: {request.ArtistaYaCobro}"), ct);

        await obras.GuardarCambiosAsync(ct);

        if (request.ArtistaYaCobro)
        {
            await adelantoService.RegistrarAsync(new RegistrarAdelantoRequest(
                obra.ArtistaId,
                request.Fecha,
                obra.Moneda,
                obra.Costo * venta.Cantidad,
                TipoAdelanto.Adelanto,
                $"Devolución de la venta #{venta.Id} (obra ya cobrada por el artista)"), ct);
        }

        return devolucion.Id;
    }
}
