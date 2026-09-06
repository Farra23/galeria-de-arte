using Galeria.Application.Auditorias;
using Galeria.Application.Obras;
using Galeria.Domain.Entities;
using Galeria.Domain.Enums;

namespace Galeria.Application.Alquileres;

public class AlquilerService(IAlquilerRepository alquileres, IObraRepository obras, AuditoriaService auditoria)
{
    public Task<List<ObraParaOperacion>> BuscarObrasDisponiblesAsync(string? texto, CancellationToken ct = default) =>
        obras.BuscarDisponiblesAsync(texto, ct);

    public Task<ObraParaOperacion?> ObtenerObraAsync(int obraId, CancellationToken ct = default) =>
        obras.ObtenerParaOperacionAsync(obraId, ct);

    public Task<List<AlquilerListItem>> BuscarAsync(AlquilerFiltro filtro, CancellationToken ct = default) =>
        alquileres.BuscarAsync(filtro, ct);

    // GRASP Information Expert en dos pasos (ver comentario en RegistrarAlquilerRequest):
    // primero el importe total del alquiler, después el reparto entre galería y artista.
    public static (decimal MontoAlquiler, decimal MontoArtista, decimal MontoGaleria) CalcularReparto(
        decimal precioVenta, decimal porcentajeAlquiler, decimal porcentajeArtista)
    {
        var montoAlquiler = Math.Round(precioVenta * porcentajeAlquiler / 100, 2, MidpointRounding.AwayFromZero);
        var montoArtista = Math.Round(montoAlquiler * porcentajeArtista / 100, 2, MidpointRounding.AwayFromZero);
        var montoGaleria = montoAlquiler - montoArtista;
        return (montoAlquiler, montoArtista, montoGaleria);
    }

    public async Task<int> RegistrarAsync(RegistrarAlquilerRequest request, CancellationToken ct = default)
    {
        var obra = await obras.ObtenerEntidadAsync(request.ObraId, ct)
            ?? throw new InvalidOperationException("La obra no existe.");

        if (!obra.EstaDisponible)
        {
            throw new InvalidOperationException("La obra no está disponible para alquilar.");
        }

        var (montoAlquiler, montoArtista, montoGaleria) =
            CalcularReparto(obra.PrecioVenta, request.PorcentajeAlquiler, request.PorcentajeArtista);

        var alquiler = new Alquiler
        {
            ObraId = obra.Id,
            FechaInicio = request.FechaInicio,
            FechaRecupero = request.FechaRecupero,
            Moneda = obra.Moneda,
            PorcentajeAlquiler = request.PorcentajeAlquiler,
            MontoAlquiler = montoAlquiler,
            MontoArtista = montoArtista,
            MontoGaleria = montoGaleria,
            Cliente = request.Cliente
        };

        await alquileres.AgregarAsync(alquiler, ct);
        await alquileres.GuardarCambiosAsync(ct);

        obra.Existencia -= 1;

        // Mismo criterio que Ventas y Retiros: si quedan más unidades del mismo código en stock,
        // no bloquearlas -- antes esto marcaba toda la obra como "Alquilada" aunque quedaran
        // unidades disponibles (ej. alquilar 1 de 4 dejaba las otras 3 invisibles para vender).
        obra.Estado = obra.Existencia > 0 ? EstadoObra.Disponible : EstadoObra.Alquilada;

        await obras.RegistrarMovimientoAsync(new Movimiento
        {
            ObraId = obra.Id,
            Fecha = request.FechaInicio,
            Tipo = TipoMovimiento.Alquiler,
            Cantidad = -1,
            Moneda = obra.Moneda,
            ReferenciaId = alquiler.Id
        }, ct);

        await auditoria.RegistrarAsync(new RegistrarAuditoriaRequest(
            Pantalla: "Alquileres",
            TipoOperacion: "Alta",
            Tabla: "Alquiler",
            ArtistaId: obra.ArtistaId,
            ObraId: obra.Id,
            EntidadId: alquiler.Id.ToString(),
            ValorNuevo: $"Alquiler {montoAlquiler} ({request.PorcentajeAlquiler}% de {obra.PrecioVenta})"), ct);

        await alquileres.GuardarCambiosAsync(ct);

        return alquiler.Id;
    }

    // Reversibilidad (requerimientos 0.4 y 7.3): mismo botón sin formularios que un retiro temporal.
    public async Task DevolverAsync(int alquilerId, CancellationToken ct = default)
    {
        var alquiler = await alquileres.ObtenerEntidadAsync(alquilerId, ct)
            ?? throw new InvalidOperationException("El alquiler no existe.");

        if (!alquiler.EstaActivo)
        {
            throw new InvalidOperationException("Este alquiler ya fue devuelto.");
        }

        var obra = await obras.ObtenerEntidadAsync(alquiler.ObraId, ct)
            ?? throw new InvalidOperationException("La obra ya no existe.");

        var hoy = DateOnly.FromDateTime(DateTime.Now);
        alquiler.FechaDevolucion = hoy;
        obra.Existencia += 1;
        obra.Estado = obra.Existencia > 0 ? EstadoObra.Disponible : EstadoObra.SinStock;

        await obras.RegistrarMovimientoAsync(new Movimiento
        {
            ObraId = obra.Id,
            Fecha = hoy,
            Tipo = TipoMovimiento.DevolucionAlquiler,
            Cantidad = 1,
            Moneda = obra.Moneda,
            ReferenciaId = alquiler.Id
        }, ct);

        await auditoria.RegistrarAsync(new RegistrarAuditoriaRequest(
            Pantalla: "Alquileres",
            TipoOperacion: "Modificacion",
            Tabla: "Alquiler",
            Columna: "FechaDevolucion",
            ValorNuevo: hoy.ToString("yyyy-MM-dd"),
            ArtistaId: obra.ArtistaId,
            ObraId: obra.Id,
            EntidadId: alquiler.Id.ToString()), ct);

        await alquileres.GuardarCambiosAsync(ct);
    }
}
