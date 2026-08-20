using Galeria.Application.Obras;
using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence.Repositories;

public class ObraRepository(GaleriaDbContext db) : IObraRepository
{
    public async Task<List<ObraListItem>> BuscarAsync(string? textoLibre, int? artistaId, CancellationToken ct = default)
    {
        var query = db.Obras.AsNoTracking().Include(o => o.Artista).AsQueryable();

        if (artistaId is not null)
        {
            query = query.Where(o => o.ArtistaId == artistaId);
        }

        if (!string.IsNullOrWhiteSpace(textoLibre))
        {
            var texto = textoLibre.Trim();
            query = query.Where(o =>
                EF.Functions.Like(o.Titulo, $"%{texto}%") ||
                EF.Functions.Like(o.Artista.Nombre, $"%{texto}%") ||
                EF.Functions.Like(o.Artista.Apellido, $"%{texto}%"));
        }

        return await query
            .OrderByDescending(o => o.FechaIngreso).ThenByDescending(o => o.Id)
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
                o.FechaIngreso))
            .ToListAsync(ct);
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

    public async Task AumentarExistenciaAsync(int obraId, int cantidad, CancellationToken ct = default)
    {
        var obra = await db.Obras.FirstAsync(o => o.Id == obraId, ct);
        obra.Existencia += cantidad;
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
