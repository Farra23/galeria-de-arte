using Galeria.Application.Obras;
using Galeria.Domain.Entities;
using Galeria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence.Repositories;

public class ObraRepository(GaleriaDbContext db) : IObraRepository
{
    public async Task<List<ObraListItem>> BuscarAsync(ObraFiltro filtro, CancellationToken ct = default)
    {
        var query = db.Obras.AsNoTracking().Include(o => o.Artista).Include(o => o.Serie).AsQueryable();

        if (filtro.ArtistaId is not null)
        {
            query = query.Where(o => o.ArtistaId == filtro.ArtistaId);
        }

        if (!string.IsNullOrWhiteSpace(filtro.TextoLibre))
        {
            var texto = filtro.TextoLibre.Trim();
            query = query.Where(o =>
                EF.Functions.Like(o.Titulo, $"%{texto}%") ||
                EF.Functions.Like(o.Artista.Nombre, $"%{texto}%") ||
                EF.Functions.Like(o.Artista.Apellido, $"%{texto}%"));
        }

        if (filtro.RubroId is not null)
        {
            query = query.Where(o => o.RubroId == filtro.RubroId);
        }

        if (filtro.TecnicaId is not null)
        {
            query = query.Where(o => o.TecnicaId == filtro.TecnicaId);
        }

        if (filtro.Moneda is not null)
        {
            query = query.Where(o => o.Moneda == filtro.Moneda);
        }

        if (filtro.TieneIVA is not null)
        {
            query = query.Where(o => o.TieneIVA == filtro.TieneIVA);
        }

        if (filtro.SoloConStock == true)
        {
            query = query.Where(o => o.Existencia > 0);
        }
        else if (filtro.SoloConStock == false)
        {
            query = query.Where(o => o.Existencia == 0);
        }

        if (filtro.Estado is not null)
        {
            query = query.Where(o => o.Estado == filtro.Estado);
        }

        if (filtro.PrecioMinimo is not null)
        {
            query = query.Where(o => o.PrecioVenta >= filtro.PrecioMinimo);
        }

        if (filtro.PrecioMaximo is not null)
        {
            query = query.Where(o => o.PrecioVenta <= filtro.PrecioMaximo);
        }

        if (filtro.FechaDesde is not null)
        {
            query = query.Where(o => o.FechaIngreso >= filtro.FechaDesde);
        }

        if (filtro.FechaHasta is not null)
        {
            query = query.Where(o => o.FechaIngreso <= filtro.FechaHasta);
        }

        if (filtro.SerieId is not null)
        {
            query = query.Where(o => o.SerieId == filtro.SerieId);
        }

        var items = await query
            .Select(o => new ObraListItem(
                o.Id,
                o.Artista.Codigo.ToString("D3") + o.NumeroObra.ToString("D3"),
                o.Titulo,
                o.Artista.Apellido + ", " + o.Artista.Nombre,
                o.Rubro != null ? o.Rubro.Nombre : null,
                o.Tecnica != null ? o.Tecnica.Nombre : null,
                o.Moneda,
                o.Costo,
                o.PrecioVenta,
                o.Existencia,
                o.Estado,
                o.FechaIngreso,
                o.PagoContado,
                o.SerieId,
                o.Serie != null ? o.Serie.Nombre : null,
                o.ImagenPrincipalPath))
            .ToListAsync(ct);

        return Ordenar(items, filtro);
    }

    public async Task<List<SerieOpcion>> ListarSeriesAsync(CancellationToken ct = default) =>
        await db.Series.AsNoTracking()
            .Include(s => s.Artista)
            .Where(s => s.Obras.Any())
            .OrderBy(s => s.Artista.Apellido).ThenBy(s => s.Nombre)
            .Select(s => new SerieOpcion(s.Id, s.Nombre, s.Artista.Apellido + ", " + s.Artista.Nombre))
            .ToListAsync(ct);

    // Orden por columna (requerimiento 0.1) resuelto en memoria a propósito: SQLite no soporta
    // ORDER BY sobre columnas decimal (Costo, PrecioVenta) en absoluto — ni siquiera el caso
    // simple, no solo el Sum-en-GroupBy ya documentado — así que se materializa primero y se
    // ordena en LINQ-to-Objects, mismo patrón que ArtistaRepository.Ordenar.
    private static List<ObraListItem> Ordenar(List<ObraListItem> items, ObraFiltro filtro)
    {
        IOrderedEnumerable<ObraListItem> Aplicar<TKey>(Func<ObraListItem, TKey> selector) =>
            filtro.OrdenDescendente ? items.OrderByDescending(selector) : items.OrderBy(selector);

        var ordenado = filtro.Orden switch
        {
            OrdenObra.Codigo => Aplicar(o => o.CodigoVisible),
            OrdenObra.Titulo => Aplicar(o => o.Titulo),
            OrdenObra.Artista => Aplicar(o => o.ArtistaNombre),
            OrdenObra.Rubro => Aplicar(o => o.Rubro ?? ""),
            OrdenObra.Tecnica => Aplicar(o => o.Tecnica ?? ""),
            OrdenObra.Costo => Aplicar(o => o.Costo),
            OrdenObra.PrecioVenta => Aplicar(o => o.PrecioVenta),
            OrdenObra.Existencia => Aplicar(o => o.Existencia),
            OrdenObra.Estado => Aplicar(o => o.Estado),
            OrdenObra.Serie => Aplicar(o => o.SerieNombre ?? ""),
            _ => Aplicar(o => o.FechaIngreso)
        };

        return ordenado.ThenByDescending(o => o.Id).ToList();
    }

    public async Task<ObraCoincidente?> BuscarCoincidenciaAsync(int artistaId, string titulo, CancellationToken ct = default)
    {
        var tituloNormalizado = titulo.ToLower();

        return await db.Obras.AsNoTracking()
            .Where(o => o.ArtistaId == artistaId && o.Titulo.ToLower() == tituloNormalizado)
            .Select(o => new ObraCoincidente(
                o.Id,
                o.Artista.Codigo.ToString("D3") + o.NumeroObra.ToString("D3"),
                o.Titulo,
                o.Existencia,
                o.Costo))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> ProximoNumeroObraAsync(int artistaId, CancellationToken ct = default)
    {
        // Correlativo por artista (decisión #1): cada artista arranca en 1, no hay contador global.
        var maximo = await db.Obras
            .Where(o => o.ArtistaId == artistaId)
            .MaxAsync(o => (int?)o.NumeroObra, ct);
        return (maximo ?? 0) + 1;
    }

    public Task AgregarAsync(Obra obra, CancellationToken ct = default)
    {
        db.Obras.Add(obra);
        return Task.CompletedTask;
    }

    public Task AgregarSerieAsync(Serie serie, CancellationToken ct = default)
    {
        db.Series.Add(serie);
        return Task.CompletedTask;
    }

    public async Task AumentarExistenciaAsync(int obraId, int cantidad, CancellationToken ct = default)
    {
        var obra = await db.Obras.FirstAsync(o => o.Id == obraId, ct);
        obra.Existencia += cantidad;
    }

    public async Task<ObraFicha?> ObtenerFichaAsync(int id, CancellationToken ct = default) =>
        await db.Obras.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new ObraFicha(
                o.Id,
                o.Artista.Codigo.ToString("D3") + o.NumeroObra.ToString("D3"),
                o.ArtistaId,
                o.Artista.Apellido + ", " + o.Artista.Nombre,
                o.Titulo,
                o.RubroId,
                o.TecnicaId,
                o.AltoCm,
                o.AnchoCm,
                o.LargoCm,
                o.Existencia,
                o.Moneda,
                o.Costo,
                o.Utilidad,
                o.TieneIVA,
                o.PrecioVenta,
                o.PagoContado,
                o.Observaciones,
                o.Estado,
                o.FechaIngreso,
                o.Serie != null ? o.Serie.Nombre : null,
                o.Serie != null ? o.Serie.Obras.Count(x => x.NumeroObra <= o.NumeroObra) : (int?)null,
                o.Serie != null ? o.Serie.Obras.Count() : (int?)null,
                o.ImagenPrincipalPath))
            .FirstOrDefaultAsync(ct);

    // Moneda y Existencia quedan afuera a propósito (ver ObraDtos.cs): no se tocan desde acá.
    public async Task ActualizarAsync(ActualizarObraRequest request, CancellationToken ct = default)
    {
        var obra = await db.Obras.FirstAsync(o => o.Id == request.Id, ct);
        obra.Titulo = request.Titulo.Trim();
        obra.RubroId = request.RubroId;
        obra.TecnicaId = request.TecnicaId;
        obra.AltoCm = request.AltoCm;
        obra.AnchoCm = request.AnchoCm;
        obra.LargoCm = request.LargoCm;
        obra.Costo = request.Costo;
        obra.Utilidad = request.Utilidad;
        obra.TieneIVA = request.TieneIVA;
        obra.PrecioVenta = request.PrecioVenta;
        obra.PagoContado = request.PagoContado;
        obra.Observaciones = request.Observaciones;
    }

    public async Task ActualizarPrecioAsync(int obraId, decimal nuevoPrecio, CancellationToken ct = default)
    {
        var obra = await db.Obras.FirstAsync(o => o.Id == obraId, ct);
        obra.PrecioVenta = nuevoPrecio;
    }

    public async Task ActualizarImagenAsync(int obraId, string? rutaRelativa, CancellationToken ct = default)
    {
        var obra = await db.Obras.FirstAsync(o => o.Id == obraId, ct);
        obra.ImagenPrincipalPath = rutaRelativa;
    }

    public Task<Obra?> ObtenerEntidadAsync(int id, CancellationToken ct = default) =>
        db.Obras.Include(o => o.Artista).FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<List<ObraParaOperacion>> BuscarDisponiblesAsync(string? texto, CancellationToken ct = default)
    {
        var query = db.Obras.AsNoTracking()
            .Include(o => o.Artista)
            .Where(o => o.Estado == EstadoObra.Disponible && o.Existencia > 0);

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var valor = texto.Trim();
            query = query.Where(o =>
                EF.Functions.Like(o.Titulo, $"%{valor}%") ||
                EF.Functions.Like(o.Artista.Nombre, $"%{valor}%") ||
                EF.Functions.Like(o.Artista.Apellido, $"%{valor}%"));
        }

        return await query
            .OrderBy(o => o.Titulo)
            .Take(15)
            .Select(o => new ObraParaOperacion(
                o.Id,
                o.Artista.Codigo.ToString("D3") + o.NumeroObra.ToString("D3"),
                o.Titulo,
                o.ArtistaId,
                o.Artista.Apellido + ", " + o.Artista.Nombre,
                o.Existencia,
                o.Moneda,
                o.Costo,
                o.PrecioVenta,
                o.TieneIVA))
            .ToListAsync(ct);
    }

    public async Task<ObraParaOperacion?> ObtenerParaOperacionAsync(int id, CancellationToken ct = default) =>
        await db.Obras.AsNoTracking()
            .Include(o => o.Artista)
            .Where(o => o.Id == id)
            .Select(o => new ObraParaOperacion(
                o.Id,
                o.Artista.Codigo.ToString("D3") + o.NumeroObra.ToString("D3"),
                o.Titulo,
                o.ArtistaId,
                o.Artista.Apellido + ", " + o.Artista.Nombre,
                o.Existencia,
                o.Moneda,
                o.Costo,
                o.PrecioVenta,
                o.TieneIVA))
            .FirstOrDefaultAsync(ct);

    public Task RegistrarMovimientoAsync(Movimiento movimiento, CancellationToken ct = default)
    {
        db.Movimientos.Add(movimiento);
        return Task.CompletedTask;
    }

    public async Task<List<MovimientoItem>> ObtenerMovimientosAsync(int obraId, CancellationToken ct = default) =>
        await db.Movimientos.AsNoTracking()
            .Where(m => m.ObraId == obraId)
            .OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id)
            .Select(m => new MovimientoItem(m.Fecha, m.Tipo, m.Cantidad, m.ReferenciaId))
            .ToListAsync(ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
