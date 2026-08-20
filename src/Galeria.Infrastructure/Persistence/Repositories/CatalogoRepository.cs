using Galeria.Application.Catalogos;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence.Repositories;

public class CatalogoRepository(GaleriaDbContext db) : ICatalogoRepository
{
    public async Task<List<OpcionCatalogo>> ListarRubrosAsync(CancellationToken ct = default) =>
        await db.Rubros.AsNoTracking()
            .Where(r => r.Activo)
            .OrderBy(r => r.Nombre)
            .Select(r => new OpcionCatalogo(r.Id, r.Nombre))
            .ToListAsync(ct);

    public async Task<List<OpcionCatalogo>> ListarTecnicasAsync(CancellationToken ct = default) =>
        await db.Tecnicas.AsNoTracking()
            .Where(t => t.Activo)
            .OrderBy(t => t.Nombre)
            .Select(t => new OpcionCatalogo(t.Id, t.Nombre))
            .ToListAsync(ct);
}
