using Galeria.Application.Catalogos;
using Galeria.Domain.Entities;
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

    public async Task<List<CatalogoItem>> ListarRubrosAdminAsync(CancellationToken ct = default) =>
        await db.Rubros.AsNoTracking()
            .OrderBy(r => r.Nombre)
            .Select(r => new CatalogoItem(r.Id, r.Nombre, r.Activo))
            .ToListAsync(ct);

    public async Task<List<CatalogoItem>> ListarTecnicasAdminAsync(CancellationToken ct = default) =>
        await db.Tecnicas.AsNoTracking()
            .OrderBy(t => t.Nombre)
            .Select(t => new CatalogoItem(t.Id, t.Nombre, t.Activo))
            .ToListAsync(ct);

    public async Task CrearRubroAsync(string nombre, CancellationToken ct = default)
    {
        db.Rubros.Add(new Rubro { Nombre = nombre });
        await db.SaveChangesAsync(ct);
    }

    public async Task CrearTecnicaAsync(string nombre, CancellationToken ct = default)
    {
        db.Tecnicas.Add(new Tecnica { Nombre = nombre });
        await db.SaveChangesAsync(ct);
    }

    public async Task ActualizarRubroAsync(int id, string nombre, bool activo, CancellationToken ct = default)
    {
        var rubro = await db.Rubros.FirstAsync(r => r.Id == id, ct);
        rubro.Nombre = nombre;
        rubro.Activo = activo;
        await db.SaveChangesAsync(ct);
    }

    public async Task ActualizarTecnicaAsync(int id, string nombre, bool activo, CancellationToken ct = default)
    {
        var tecnica = await db.Tecnicas.FirstAsync(t => t.Id == id, ct);
        tecnica.Nombre = nombre;
        tecnica.Activo = activo;
        await db.SaveChangesAsync(ct);
    }
}
