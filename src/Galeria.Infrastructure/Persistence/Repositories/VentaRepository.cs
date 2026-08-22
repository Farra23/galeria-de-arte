using Galeria.Application.Ventas;
using Galeria.Domain.Entities;
using Galeria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence.Repositories;

public class VentaRepository(GaleriaDbContext db) : IVentaRepository
{
    public Task AgregarAsync(Venta venta, CancellationToken ct = default)
    {
        db.Ventas.Add(venta);
        return Task.CompletedTask;
    }

    public Task<Venta?> ObtenerEntidadAsync(int id, CancellationToken ct = default) =>
        db.Ventas.FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<List<VentaListItem>> BuscarAsync(VentaFiltro filtro, CancellationToken ct = default)
    {
        var query = db.Ventas.AsNoTracking()
            .Include(v => v.Obra).ThenInclude(o => o.Artista)
            .Include(v => v.Certificado)
            .AsQueryable();

        if (filtro.ArtistaId is not null)
        {
            query = query.Where(v => v.Obra.ArtistaId == filtro.ArtistaId);
        }

        if (filtro.Moneda is not null)
        {
            query = query.Where(v => v.Moneda == filtro.Moneda);
        }

        if (filtro.FechaDesde is not null)
        {
            query = query.Where(v => v.Fecha >= filtro.FechaDesde);
        }

        if (filtro.FechaHasta is not null)
        {
            query = query.Where(v => v.Fecha <= filtro.FechaHasta);
        }

        if (!string.IsNullOrWhiteSpace(filtro.TextoLibre))
        {
            var texto = filtro.TextoLibre.Trim();
            query = query.Where(v =>
                EF.Functions.Like(v.Obra.Titulo, $"%{texto}%") ||
                EF.Functions.Like(v.Obra.Artista.Nombre, $"%{texto}%") ||
                EF.Functions.Like(v.Obra.Artista.Apellido, $"%{texto}%"));
        }

        var items = await query
            .Select(v => new VentaListItem(
                v.Id,
                v.Fecha,
                v.Obra.Artista.Codigo.ToString("D3") + v.Obra.NumeroObra.ToString("D3"),
                v.Obra.Titulo,
                v.Obra.Artista.Apellido + ", " + v.Obra.Artista.Nombre,
                v.Cantidad,
                v.Moneda,
                v.PrecioVenta,
                v.Certificado != null))
            .ToListAsync(ct);

        return Ordenar(items, filtro);
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    // En memoria a propósito: SQLite no soporta ORDER BY sobre columnas decimal (Precio) — ver
    // la misma nota en ObraRepository.Ordenar.
    private static List<VentaListItem> Ordenar(List<VentaListItem> items, VentaFiltro filtro)
    {
        IOrderedEnumerable<VentaListItem> Aplicar<TKey>(Func<VentaListItem, TKey> selector) =>
            filtro.OrdenDescendente ? items.OrderByDescending(selector) : items.OrderBy(selector);

        var ordenado = filtro.Orden switch
        {
            OrdenVenta.Codigo => Aplicar(v => v.CodigoObra),
            OrdenVenta.Titulo => Aplicar(v => v.Titulo),
            OrdenVenta.Artista => Aplicar(v => v.ArtistaNombre),
            OrdenVenta.Cantidad => Aplicar(v => v.Cantidad),
            OrdenVenta.Precio => Aplicar(v => v.PrecioVenta),
            _ => Aplicar(v => v.Fecha)
        };

        return ordenado.ThenByDescending(v => v.Id).ToList();
    }
}
