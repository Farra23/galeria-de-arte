using Galeria.Application.Artistas;
using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence.Repositories;

public class ArtistaRepository(GaleriaDbContext db) : IArtistaRepository
{
    public async Task<List<ArtistaListItem>> BuscarAsync(string? textoLibre, CancellationToken ct = default)
    {
        var query = db.Artistas.AsNoTracking().Where(a => a.Activo);

        if (!string.IsNullOrWhiteSpace(textoLibre))
        {
            var texto = textoLibre.Trim();
            query = query.Where(a =>
                EF.Functions.Like(a.Nombre, $"%{texto}%") ||
                EF.Functions.Like(a.Apellido, $"%{texto}%") ||
                EF.Functions.Like(a.Taller ?? "", $"%{texto}%"));
        }

        // Concatenamos acá en vez de reusar Artista.NombreCompleto: es una propiedad calculada
        // en memoria, no hay garantía de que EF la traduzca a SQL dentro del Select.
        return await query
            .OrderBy(a => a.Apellido).ThenBy(a => a.Nombre)
            .Select(a => new ArtistaListItem(
                a.Id,
                a.Codigo,
                a.Apellido + ", " + a.Nombre,
                a.Taller,
                a.Celular,
                a.Correo,
                a.Obras.Count))
            .ToListAsync(ct);
    }

    public async Task<List<ArtistaOpcion>> ListarActivosAsync(CancellationToken ct = default) =>
        await db.Artistas.AsNoTracking()
            .Where(a => a.Activo)
            .OrderBy(a => a.Apellido).ThenBy(a => a.Nombre)
            .Select(a => new ArtistaOpcion(a.Id, a.Codigo, a.Apellido + ", " + a.Nombre))
            .ToListAsync(ct);

    public async Task<int> ProximoCodigoAsync(CancellationToken ct = default)
    {
        var maximo = await db.Artistas.MaxAsync(a => (int?)a.Codigo, ct);
        return (maximo ?? 0) + 1;
    }

    public Task AgregarAsync(Artista artista, CancellationToken ct = default)
    {
        db.Artistas.Add(artista);
        return Task.CompletedTask;
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
