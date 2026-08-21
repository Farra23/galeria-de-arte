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

        return await query
            .OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Id)
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
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
