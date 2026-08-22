using Galeria.Application.Alquileres;
using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence.Repositories;

public class AlquilerRepository(GaleriaDbContext db) : IAlquilerRepository
{
    public Task AgregarAsync(Alquiler alquiler, CancellationToken ct = default)
    {
        db.Alquileres.Add(alquiler);
        return Task.CompletedTask;
    }

    public Task<Alquiler?> ObtenerEntidadAsync(int id, CancellationToken ct = default) =>
        db.Alquileres.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<List<AlquilerListItem>> BuscarAsync(AlquilerFiltro filtro, CancellationToken ct = default)
    {
        var query = db.Alquileres.AsNoTracking()
            .Include(a => a.Obra).ThenInclude(o => o.Artista)
            .AsQueryable();

        if (filtro.ArtistaId is not null)
        {
            query = query.Where(a => a.Obra.ArtistaId == filtro.ArtistaId);
        }

        if (filtro.SoloActivos == true)
        {
            query = query.Where(a => a.FechaDevolucion == null);
        }

        if (!string.IsNullOrWhiteSpace(filtro.TextoLibre))
        {
            var texto = filtro.TextoLibre.Trim();
            query = query.Where(a =>
                EF.Functions.Like(a.Obra.Titulo, $"%{texto}%") ||
                EF.Functions.Like(a.Obra.Artista.Nombre, $"%{texto}%") ||
                EF.Functions.Like(a.Obra.Artista.Apellido, $"%{texto}%") ||
                (a.Cliente != null && EF.Functions.Like(a.Cliente, $"%{texto}%")));
        }

        var hoy = DateOnly.FromDateTime(DateTime.Now);

        var items = await query
            .Select(a => new AlquilerListItem(
                a.Id,
                a.FechaInicio,
                a.Obra.Artista.Codigo.ToString("D3") + a.Obra.NumeroObra.ToString("D3"),
                a.Obra.Titulo,
                a.Obra.Artista.Apellido + ", " + a.Obra.Artista.Nombre,
                a.Cliente,
                a.Moneda,
                a.MontoAlquiler,
                a.MontoArtista,
                a.FechaDevolucion == null,
                a.FechaDevolucion == null && a.FechaRecupero != null && a.FechaRecupero < hoy,
                a.FechaRecupero))
            .ToListAsync(ct);

        return Ordenar(items, filtro);
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    // En memoria a propósito: SQLite no soporta ORDER BY sobre columnas decimal (Monto,
    // MontoArtista) — ver la misma nota en ObraRepository.Ordenar.
    private static List<AlquilerListItem> Ordenar(List<AlquilerListItem> items, AlquilerFiltro filtro)
    {
        IOrderedEnumerable<AlquilerListItem> Aplicar<TKey>(Func<AlquilerListItem, TKey> selector) =>
            filtro.OrdenDescendente ? items.OrderByDescending(selector) : items.OrderBy(selector);

        var ordenado = filtro.Orden switch
        {
            OrdenAlquiler.Codigo => Aplicar(a => a.CodigoObra),
            OrdenAlquiler.Obra => Aplicar(a => a.Titulo),
            OrdenAlquiler.Artista => Aplicar(a => a.ArtistaNombre),
            OrdenAlquiler.Cliente => Aplicar(a => a.Cliente ?? ""),
            OrdenAlquiler.Monto => Aplicar(a => a.MontoAlquiler),
            OrdenAlquiler.MontoArtista => Aplicar(a => a.MontoArtista),
            _ => Aplicar(a => a.FechaInicio)
        };

        return ordenado.ThenByDescending(a => a.Id).ToList();
    }
}
