using Galeria.Application.Auditorias;
using Galeria.Application.Obras;
using Galeria.Application.Parametros;
using Galeria.Domain.Entities;
using Galeria.Domain.Enums;

namespace Galeria.Application.Ventas;

public class VentaService(IVentaRepository ventas, IObraRepository obras, IParametroRepository parametros, AuditoriaService auditoria)
{
    public Task<List<ObraParaVenta>> BuscarObrasDisponiblesAsync(string? texto, CancellationToken ct = default) =>
        ventas.BuscarObrasDisponiblesAsync(texto, ct);

    public Task<ObraParaVenta?> ObtenerObraParaVentaAsync(int obraId, CancellationToken ct = default) =>
        ventas.ObtenerObraParaVentaAsync(obraId, ct);

    public Task<List<VentaListItem>> BuscarAsync(VentaFiltro filtro, CancellationToken ct = default) =>
        ventas.BuscarAsync(filtro, ct);

    // Punto único donde una venta afecta el stock (requerimiento 0.5: la obra pasa a Sin stock
    // cuando la existencia llega a 0) y deja rastro en el libro de movimientos y en Auditoría.
    public async Task<int> RegistrarAsync(RegistrarVentaRequest request, CancellationToken ct = default)
    {
        var obra = await obras.ObtenerEntidadAsync(request.ObraId, ct)
            ?? throw new InvalidOperationException("La obra no existe.");

        if (!obra.EstaDisponible)
        {
            throw new InvalidOperationException("La obra no está disponible para la venta.");
        }

        if (request.Cantidad < 1 || request.Cantidad > obra.Existencia)
        {
            throw new InvalidOperationException("La cantidad a vender supera la existencia disponible.");
        }

        var iva = await ObtenerParametroAsync(Parametro.Claves.IvaPorcentaje, 22m, ct);
        var claveRedondeo = obra.Moneda == Moneda.Pesos ? Parametro.Claves.RedondeosPesos : Parametro.Claves.RedondeoDolar;
        var redondeo = await ObtenerParametroAsync(claveRedondeo, 1m, ct);

        var venta = new Venta
        {
            ObraId = obra.Id,
            Fecha = request.Fecha,
            Cantidad = request.Cantidad,
            Moneda = request.Moneda,
            PrecioVenta = request.PrecioVenta,
            PrecioCalculado = obra.CalcularPrecioVenta(iva, redondeo),
            ExentaIVA = request.ExentaIVA,
            Observaciones = request.Observaciones
        };

        await ventas.AgregarAsync(venta, ct);
        await ventas.GuardarCambiosAsync(ct); // necesito venta.Id (autonumérico) antes de seguir

        obra.Existencia -= request.Cantidad;
        if (obra.Existencia <= 0)
        {
            obra.Estado = EstadoObra.SinStock;
        }

        await obras.RegistrarMovimientoAsync(new Movimiento
        {
            ObraId = obra.Id,
            Fecha = request.Fecha,
            Tipo = TipoMovimiento.Venta,
            Cantidad = -request.Cantidad,
            Moneda = request.Moneda,
            ReferenciaId = venta.Id
        }, ct);

        await auditoria.RegistrarAsync(new RegistrarAuditoriaRequest(
            Pantalla: "Ventas",
            TipoOperacion: "Alta",
            Tabla: "Venta",
            ArtistaId: obra.ArtistaId,
            ObraId: obra.Id,
            EntidadId: venta.Id.ToString(),
            ValorNuevo: $"{request.Cantidad} unidad(es) a {request.PrecioVenta}"), ct);

        await ventas.GuardarCambiosAsync(ct);

        return venta.Id;
    }

    private async Task<decimal> ObtenerParametroAsync(string clave, decimal porDefecto, CancellationToken ct)
    {
        var texto = await parametros.ObtenerAsync(clave, ct);
        return decimal.TryParse(texto, out var valor) ? valor : porDefecto;
    }
}
