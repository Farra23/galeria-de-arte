using Galeria.Application.Ventas;
using Galeria.Domain.Entities;
using Galeria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence.Repositories;

public class VentaRepository(GaleriaDbContext db) : IVentaRepository
{
    public async Task<List<ObraParaVenta>> BuscarObrasDisponiblesAsync(string? texto, CancellationToken ct = default)
    {
        var query = db.Obras.AsNoTracking()
            .Include(o => o.Artista)
            .Where(o => o.Estado == EstadoObra.Disponible && o.Existencia > 0);

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var valor = texto.Trim();
            query = query.Where(o =>
                EF.Functions.Like(o.Titulo, $"%{valor}%") ||
                EF.Functions.Like(o.Artista.Nombre, $"%{valor}%") ||
                EF.Functions.Like(o.Artista.Apellido, $"%{valor}%"));
        }

        return await query
            .OrderBy(o => o.Titulo)
            .Take(15)
            .Select(o => new ObraParaVenta(
                o.Id,
                o.Artista.Codigo.ToString("D3") + o.NumeroObra.ToString("D3"),
                o.Titulo,
                o.Artista.Apellido + ", " + o.Artista.Nombre,
                o.Existencia,
                o.Moneda,
                o.Costo,
                o.PrecioVenta,
                o.TieneIVA))
            .ToListAsync(ct);
    }

    public async Task<ObraParaVenta?> ObtenerObraParaVentaAsync(int obraId, CancellationToken ct = default) =>
        await db.Obras.AsNoTracking()
            .Include(o => o.Artista)
            .Where(o => o.Id == obraId)
            .Select(o => new ObraParaVenta(
                o.Id,
                o.Artista.Codigo.ToString("D3") + o.NumeroObra.ToString("D3"),
                o.Titulo,
                o.Artista.Apellido + ", " + o.Artista.Nombre,
                o.Existencia,
                o.Moneda,
                o.Costo,
                o.PrecioVenta,
                o.TieneIVA))
            .FirstOrDefaultAsync(ct);

    public Task AgregarAsync(Venta venta, CancellationToken ct = default)
    {
        db.Ventas.Add(venta);
        return Task.CompletedTask;
    }

    public async Task<List<VentaListItem>> BuscarAsync(VentaFiltro filtro, CancellationToken ct = default)
    {
        var query = db.Ventas.AsNoTracking()
            .Include(v => v.Obra).ThenInclude(o => o.Artista)
            .Include(v => v.Certificado)
            .AsQueryable();

        if (filtro.ArtistaId is not null)
        {
            query = query.Where(v => v.Obra.ArtistaId == filtro.ArtistaId);
        }

        if (filtro.Moneda is not null)
        {
            query = query.Where(v => v.Moneda == filtro.Moneda);
        }

        if (filtro.FechaDesde is not null)
        {
            query = query.Where(v => v.Fecha >= filtro.FechaDesde);
        }

        if (filtro.FechaHasta is not null)
        {
            query = query.Where(v => v.Fecha <= filtro.FechaHasta);
        }

        if (!string.IsNullOrWhiteSpace(filtro.TextoLibre))
        {
            var texto = filtro.TextoLibre.Trim();
            query = query.Where(v =>
                EF.Functions.Like(v.Obra.Titulo, $"%{texto}%") ||
                EF.Functions.Like(v.Obra.Artista.Nombre, $"%{texto}%") ||
                EF.Functions.Like(v.Obra.Artista.Apellido, $"%{texto}%"));
        }

        return await query
            .OrderByDescending(v => v.Fecha).ThenByDescending(v => v.Id)
            .Select(v => new VentaListItem(
                v.Id,
                v.Fecha,
                v.Obra.Artista.Codigo.ToString("D3") + v.Obra.NumeroObra.ToString("D3"),
                v.Obra.Titulo,
                v.Obra.Artista.Apellido + ", " + v.Obra.Artista.Nombre,
                v.Cantidad,
                v.Moneda,
                v.PrecioVenta,
                v.Certificado != null))
            .ToListAsync(ct);
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
