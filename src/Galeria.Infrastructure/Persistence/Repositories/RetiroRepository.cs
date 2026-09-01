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
                r.Obra.Artista.Celular,
                r.Obra.ArtistaId))
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

        return await Ordenar(query, filtro)
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
                r.Obra.Artista.Celular,
                r.Obra.ArtistaId))
            .ToListAsync(ct);
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    private static IQueryable<Retiro> Ordenar(IQueryable<Retiro> query, RetiroFiltro filtro) => filtro.Orden switch
    {
        OrdenRetiro.Artista => filtro.OrdenDescendente
            ? query.OrderByDescending(r => r.Obra.Artista.Apellido).ThenByDescending(r => r.Obra.Artista.Nombre)
            : query.OrderBy(r => r.Obra.Artista.Apellido).ThenBy(r => r.Obra.Artista.Nombre),
        OrdenRetiro.Codigo => filtro.OrdenDescendente
            ? query.OrderByDescending(r => r.Obra.Artista.Codigo).ThenByDescending(r => r.Obra.NumeroObra)
            : query.OrderBy(r => r.Obra.Artista.Codigo).ThenBy(r => r.Obra.NumeroObra),
        OrdenRetiro.Obra => filtro.OrdenDescendente
            ? query.OrderByDescending(r => r.Obra.Titulo)
            : query.OrderBy(r => r.Obra.Titulo),
        OrdenRetiro.Tipo => filtro.OrdenDescendente
            ? query.OrderByDescending(r => r.Tipo)
            : query.OrderBy(r => r.Tipo),
        OrdenRetiro.Motivo => filtro.OrdenDescendente
            ? query.OrderByDescending(r => r.Motivo)
            : query.OrderBy(r => r.Motivo),
        _ => filtro.OrdenDescendente
            ? query.OrderByDescending(r => r.Fecha).ThenByDescending(r => r.Id)
            : query.OrderBy(r => r.Fecha).ThenBy(r => r.Id)
    };
}
