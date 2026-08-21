using Galeria.Application.Devoluciones;
using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence.Repositories;

public class DevolucionRepository(GaleriaDbContext db) : IDevolucionRepository
{
    public async Task<List<VentaParaDevolucion>> BuscarVentasAsync(string? texto, CancellationToken ct = default)
    {
        var query = db.Ventas.AsNoTracking()
            .Include(v => v.Obra).ThenInclude(o => o.Artista)
            .Where(v => v.Devolucion == null);

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var valor = texto.Trim();
            query = query.Where(v =>
                EF.Functions.Like(v.Obra.Titulo, $"%{valor}%") ||
                EF.Functions.Like(v.Obra.Artista.Nombre, $"%{valor}%") ||
                EF.Functions.Like(v.Obra.Artista.Apellido, $"%{valor}%"));
        }

        return await query
            .OrderByDescending(v => v.Fecha).ThenByDescending(v => v.Id)
            .Take(15)
            .Select(v => new VentaParaDevolucion(
                v.Id,
                v.Fecha,
                v.Obra.Artista.Codigo.ToString("D3") + v.Obra.NumeroObra.ToString("D3"),
                v.Obra.Titulo,
                v.Obra.Artista.Apellido + ", " + v.Obra.Artista.Nombre,
                v.Cantidad,
                v.Moneda,
                v.PrecioVenta))
            .ToListAsync(ct);
    }

    public Task AgregarAsync(Devolucion devolucion, CancellationToken ct = default)
    {
        db.Devoluciones.Add(devolucion);
        return Task.CompletedTask;
    }

    public async Task<List<DevolucionListItem>> BuscarAsync(DevolucionFiltro filtro, CancellationToken ct = default)
    {
        var query = db.Devoluciones.AsNoTracking()
            .Include(d => d.Venta).ThenInclude(v => v.Obra).ThenInclude(o => o.Artista)
            .AsQueryable();

        if (filtro.ArtistaId is not null)
        {
            query = query.Where(d => d.Venta.Obra.ArtistaId == filtro.ArtistaId);
        }

        if (!string.IsNullOrWhiteSpace(filtro.TextoLibre))
        {
            var texto = filtro.TextoLibre.Trim();
            query = query.Where(d =>
                EF.Functions.Like(d.Venta.Obra.Titulo, $"%{texto}%") ||
                EF.Functions.Like(d.Venta.Obra.Artista.Nombre, $"%{texto}%") ||
                EF.Functions.Like(d.Venta.Obra.Artista.Apellido, $"%{texto}%"));
        }

        return await query
            .OrderByDescending(d => d.Fecha).ThenByDescending(d => d.Id)
            .Select(d => new DevolucionListItem(
                d.Id,
                d.Fecha,
                d.Venta.Obra.Artista.Codigo.ToString("D3") + d.Venta.Obra.NumeroObra.ToString("D3"),
                d.Venta.Obra.Titulo,
                d.Venta.Obra.Artista.Apellido + ", " + d.Venta.Obra.Artista.Nombre,
                d.Motivo,
                d.ArtistaYaCobro))
            .ToListAsync(ct);
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
