using Galeria.Application.Retiros;
using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence.Repositories;

public class RetiroRepository(GaleriaDbContext db) : IRetiroRepository
{
    public Task AgregarAsync(Retiro retiro, CancellationToken ct = default)
    {
        db.Retiros.Add(retiro);
        return Task.CompletedTask;
    }

    public Task<Retiro?> ObtenerEntidadAsync(int id, CancellationToken ct = default) =>
        db.Retiros.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<RetiroListItem?> ObtenerAsync(int id, CancellationToken ct = default)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);

        return await db.Retiros.AsNoTracking()
            .Include(r => r.Obra).ThenInclude(o => o.Artista)
            .Where(r => r.Id == id)
            .Select(r => new RetiroListItem(
                r.Id,
                r.Fecha,
                r.Obra.Artista.Codigo.ToString("D3") + r.Obra.NumeroObra.ToString("D3"),
                r.Obra.Titulo,
                r.Obra.Artista.Apellido + ", " + r.Obra.Artista.Nombre,
                r.Tipo,
                r.Motivo,
                r.FechaDevolucion != null,
                r.FechaEstimadaDevolucion,
                r.FechaDevolucion == null && r.FechaEstimadaDevolucion != null && r.FechaEstimadaDevolucion < hoy,
                r.Obra.Artista.Correo,
                r.Obra.Artista.Celular))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<RetiroListItem>> BuscarAsync(RetiroFiltro filtro, CancellationToken ct = default)
    {
        var query = db.Retiros.AsNoTracking()
            .Include(r => r.Obra).ThenInclude(o => o.Artista)
            .AsQueryable();

        if (filtro.ArtistaId is not null)
        {
            query = query.Where(r => r.Obra.ArtistaId == filtro.ArtistaId);
        }

        if (filtro.Tipo is not null)
        {
            query = query.Where(r => r.Tipo == filtro.Tipo);
        }

        if (filtro.SoloActivos == true)
        {
            query = query.Where(r => r.FechaDevolucion == null);
        }

        if (!string.IsNullOrWhiteSpace(filtro.TextoLibre))
        {
            var texto = filtro.TextoLibre.Trim();
            query = query.Where(r =>
                EF.Functions.Like(r.Obra.Titulo, $"%{texto}%") ||
                EF.Functions.Like(r.Obra.Artista.Nombre, $"%{texto}%") ||
                EF.Functions.Like(r.Obra.Artista.Apellido, $"%{texto}%"));
        }

        var hoy = DateOnly.FromDateTime(DateTime.Now);

        return await query
            .OrderByDescending(r => r.Fecha).ThenByDescending(r => r.Id)
            .Select(r => new RetiroListItem(
                r.Id,
                r.Fecha,
                r.Obra.Artista.Codigo.ToString("D3") + r.Obra.NumeroObra.ToString("D3"),
                r.Obra.Titulo,
                r.Obra.Artista.Apellido + ", " + r.Obra.Artista.Nombre,
                r.Tipo,
                r.Motivo,
                r.FechaDevolucion != null,
                r.FechaEstimadaDevolucion,
                r.FechaDevolucion == null && r.FechaEstimadaDevolucion != null && r.FechaEstimadaDevolucion < hoy,
                r.Obra.Artista.Correo,
                r.Obra.Artista.Celular))
            .ToListAsync(ct);
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
