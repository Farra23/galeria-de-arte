using Galeria.Application.Adelantos;
using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence.Repositories;

public class AdelantoRepository(GaleriaDbContext db) : IAdelantoRepository
{
    public Task AgregarAsync(Adelanto adelanto, CancellationToken ct = default)
    {
        db.Adelantos.Add(adelanto);
        return Task.CompletedTask;
    }

    public async Task<List<AdelantoListItem>> BuscarAsync(AdelantoFiltro filtro, CancellationToken ct = default)
    {
        var query = db.Adelantos.AsNoTracking().Include(a => a.Artista).AsQueryable();

        if (filtro.ArtistaId is not null)
        {
            query = query.Where(a => a.ArtistaId == filtro.ArtistaId);
        }

        if (filtro.FechaDesde is not null)
        {
            query = query.Where(a => a.Fecha >= filtro.FechaDesde);
        }

        if (filtro.FechaHasta is not null)
        {
            query = query.Where(a => a.Fecha <= filtro.FechaHasta);
        }

        var items = await query
            .Select(a => new AdelantoListItem(
                a.Id,
                a.Fecha,
                a.Artista.Apellido + ", " + a.Artista.Nombre,
                a.Moneda,
                a.Importe,
                a.Tipo,
                a.Observaciones,
                a.LiquidacionId != null,
                a.LiquidacionId))
            .ToListAsync(ct);

        return Ordenar(items, filtro);
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    // En memoria a propósito: SQLite no soporta ORDER BY sobre columnas decimal (Importe) — ver
    // la misma nota en ObraRepository.Ordenar.
    private static List<AdelantoListItem> Ordenar(List<AdelantoListItem> items, AdelantoFiltro filtro)
    {
        IOrderedEnumerable<AdelantoListItem> Aplicar<TKey>(Func<AdelantoListItem, TKey> selector) =>
            filtro.OrdenDescendente ? items.OrderByDescending(selector) : items.OrderBy(selector);

        var ordenado = filtro.Orden switch
        {
            OrdenAdelanto.Artista => Aplicar(a => a.ArtistaNombre),
            OrdenAdelanto.Tipo => Aplicar(a => a.Tipo),
            OrdenAdelanto.Importe => Aplicar(a => a.Importe),
            _ => Aplicar(a => a.Fecha)
        };

        return ordenado.ThenByDescending(a => a.Id).ToList();
    }
}
