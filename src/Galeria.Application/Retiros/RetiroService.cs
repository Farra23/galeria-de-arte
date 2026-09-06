using Galeria.Application.Auditorias;
using Galeria.Application.Obras;
using Galeria.Domain.Entities;
using Galeria.Domain.Enums;

namespace Galeria.Application.Retiros;

public class RetiroService(IRetiroRepository retiros, IObraRepository obras, AuditoriaService auditoria)
{
    public Task<List<ObraParaOperacion>> BuscarObrasDisponiblesAsync(string? texto, CancellationToken ct = default) =>
        obras.BuscarDisponiblesAsync(texto, ct);

    public Task<ObraParaOperacion?> ObtenerObraAsync(int obraId, CancellationToken ct = default) =>
        obras.ObtenerParaOperacionAsync(obraId, ct);

    public Task<List<RetiroListItem>> BuscarAsync(RetiroFiltro filtro, CancellationToken ct = default) =>
        retiros.BuscarAsync(filtro, ct);

    public Task<RetiroListItem?> ObtenerAsync(int id, CancellationToken ct = default) =>
        retiros.ObtenerAsync(id, ct);

    public async Task<int> RegistrarAsync(RegistrarRetiroRequest request, CancellationToken ct = default)
    {
        var obra = await obras.ObtenerEntidadAsync(request.ObraId, ct)
            ?? throw new InvalidOperationException("La obra no existe.");

        if (!obra.EstaDisponible)
        {
            throw new InvalidOperationException("La obra no está disponible para retirar.");
        }

        if (request.Cantidad < 1 || request.Cantidad > obra.Existencia)
        {
            throw new InvalidOperationException("La cantidad a retirar supera la existencia disponible.");
        }

        var retiro = new Retiro
        {
            ObraId = obra.Id,
            Fecha = request.Fecha,
            Cantidad = request.Cantidad,
            Tipo = request.Tipo,
            Motivo = request.Motivo,
            // Fecha estimada de devolución (0.4 "+"): no aplica a un retiro definitivo.
            FechaEstimadaDevolucion = request.Tipo == TipoRetiro.Temporal ? request.FechaEstimadaDevolucion : null
        };

        await retiros.AgregarAsync(retiro, ct);
        await retiros.GuardarCambiosAsync(ct);

        obra.Existencia -= request.Cantidad;

        // Si la obra tiene varias unidades bajo el mismo código (Existencia > 1), retirar solo
        // parte del lote no debe bloquear las que quedan -- mismo criterio que ya usa Ventas
        // (EstadoObra.SinStock recién cuando Existencia llega a 0). Antes esto pisaba el estado
        // sin mirar Existencia: retirar 1 de 4 dejaba las otras 3 marcadas como no disponibles.
        obra.Estado = obra.Existencia > 0
            ? EstadoObra.Disponible
            : request.Tipo == TipoRetiro.Temporal ? EstadoObra.RetiradaTemporal : EstadoObra.RetiradaDefinitiva;

        await obras.RegistrarMovimientoAsync(new Movimiento
        {
            ObraId = obra.Id,
            Fecha = request.Fecha,
            Tipo = request.Tipo == TipoRetiro.Temporal ? TipoMovimiento.RetiroTemporal : TipoMovimiento.RetiroDefinitivo,
            Cantidad = -request.Cantidad,
            Moneda = obra.Moneda,
            ReferenciaId = retiro.Id
        }, ct);

        await auditoria.RegistrarAsync(new RegistrarAuditoriaRequest(
            Pantalla: "Retiros",
            TipoOperacion: "Alta",
            Tabla: "Retiro",
            ArtistaId: obra.ArtistaId,
            ObraId: obra.Id,
            EntidadId: retiro.Id.ToString(),
            ValorNuevo: $"{request.Tipo} - {request.Cantidad} unidad(es)"), ct);

        await retiros.GuardarCambiosAsync(ct);

        return retiro.Id;
    }

    // Reversibilidad (requerimientos 0.4 y 6.3): un retiro temporal vuelve al stock con un solo
    // botón, sin formularios. Un retiro definitivo no tiene vuelta atrás desde acá.
    public async Task DevolverAsync(int retiroId, CancellationToken ct = default)
    {
        var retiro = await retiros.ObtenerEntidadAsync(retiroId, ct)
            ?? throw new InvalidOperationException("El retiro no existe.");

        if (retiro.Tipo != TipoRetiro.Temporal)
        {
            throw new InvalidOperationException("Un retiro definitivo no se puede devolver.");
        }

        if (retiro.EstaDevuelto)
        {
            throw new InvalidOperationException("Este retiro ya fue devuelto.");
        }

        var obra = await obras.ObtenerEntidadAsync(retiro.ObraId, ct)
            ?? throw new InvalidOperationException("La obra ya no existe.");

        var hoy = DateOnly.FromDateTime(DateTime.Now);
        retiro.FechaDevolucion = hoy;
        obra.Existencia += retiro.Cantidad;
        obra.Estado = obra.Existencia > 0 ? EstadoObra.Disponible : EstadoObra.SinStock;

        await obras.RegistrarMovimientoAsync(new Movimiento
        {
            ObraId = obra.Id,
            Fecha = hoy,
            Tipo = TipoMovimiento.DevolucionTemporal,
            Cantidad = retiro.Cantidad,
            Moneda = obra.Moneda,
            ReferenciaId = retiro.Id
        }, ct);

        await auditoria.RegistrarAsync(new RegistrarAuditoriaRequest(
            Pantalla: "Retiros",
            TipoOperacion: "Modificacion",
            Tabla: "Retiro",
            Columna: "FechaDevolucion",
            ValorNuevo: hoy.ToString("yyyy-MM-dd"),
            ArtistaId: obra.ArtistaId,
            ObraId: obra.Id,
            EntidadId: retiro.Id.ToString()), ct);

        await retiros.GuardarCambiosAsync(ct);
    }
}
