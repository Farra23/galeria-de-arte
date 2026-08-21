using Galeria.Application.Artistas;
using Galeria.Domain.Entities;
using Galeria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence.Repositories;

public class ArtistaRepository(GaleriaDbContext db) : IArtistaRepository
{
    public async Task<List<ArtistaListItem>> BuscarAsync(ArtistaFiltro filtro, CancellationToken ct = default)
    {
        var query = db.Artistas.AsNoTracking().Where(a => a.Activo);

        if (!string.IsNullOrWhiteSpace(filtro.TextoLibre))
        {
            var texto = filtro.TextoLibre.Trim();
            query = query.Where(a =>
                EF.Functions.Like(a.Nombre, $"%{texto}%") ||
                EF.Functions.Like(a.Apellido, $"%{texto}%") ||
                EF.Functions.Like(a.Taller ?? "", $"%{texto}%"));
        }

        var artistas = await query
            .Select(a => new
            {
                a.Id,
                a.Codigo,
                Nombre = a.Apellido + ", " + a.Nombre,
                a.Taller,
                a.Celular,
                a.Correo,
                CantidadObras = a.Obras.Count,
                ObrasEnStock = a.Obras.Count(o => o.Existencia > 0)
            })
            .ToListAsync(ct);

        // Saldo pendiente por artista y moneda (columna "saldo a pagar" del requerimiento 4.1):
        // misma lógica que el motor de Liquidaciones (ver LiquidacionRepository.BuscarPendientesAsync)
        // pero en tres consultas agregadas para toda la lista en vez de un N+1 por artista.
        var ventaIdsLiquidados = await db.LineasLiquidacion
            .Where(l => (l.Tipo == TipoLinea.Venta || l.Tipo == TipoLinea.PagoContado || l.Tipo == TipoLinea.Devolucion)
                && l.ReferenciaId != null)
            .Select(l => l.ReferenciaId!.Value)
            .ToListAsync(ct);

        var alquilerIdsLiquidados = await db.LineasLiquidacion
            .Where(l => l.Tipo == TipoLinea.Alquiler && l.ReferenciaId != null)
            .Select(l => l.ReferenciaId!.Value)
            .ToListAsync(ct);

        // El SUM de SQLite no soporta decimal en la traducción de EF Core — se trae el detalle
        // (sin agregar) y se agrupa/suma en memoria, mismo criterio que ya usa el motor de
        // Liquidaciones (LiquidacionRepository.BuscarPendientesAsync + CalcularTotales).
        var ventasCandidatas = await db.Ventas.AsNoTracking()
            .Where(v => !ventaIdsLiquidados.Contains(v.Id) && !v.Obra.PagoContado && v.Devolucion == null)
            .Select(v => new { v.Obra.ArtistaId, v.Moneda, Monto = v.Obra.Costo * v.Cantidad })
            .ToListAsync(ct);

        var alquileresCandidatos = await db.Alquileres.AsNoTracking()
            .Where(a => !alquilerIdsLiquidados.Contains(a.Id))
            .Select(a => new { a.Obra.ArtistaId, a.Moneda, Monto = a.MontoArtista })
            .ToListAsync(ct);

        var adelantosCandidatos = await db.Adelantos.AsNoTracking()
            .Where(ad => ad.LiquidacionId == null)
            .Select(ad => new { ad.ArtistaId, ad.Moneda, ad.Tipo, ad.Importe })
            .ToListAsync(ct);

        var saldoVentas = ventasCandidatas
            .GroupBy(v => (v.ArtistaId, v.Moneda))
            .Select(g => new { ArtistaId = g.Key.ArtistaId, g.Key.Moneda, Monto = g.Sum(v => v.Monto) })
            .ToList();

        var saldoAlquileres = alquileresCandidatos
            .GroupBy(a => (a.ArtistaId, a.Moneda))
            .Select(g => new { ArtistaId = g.Key.ArtistaId, g.Key.Moneda, Monto = g.Sum(a => a.Monto) })
            .ToList();

        var saldoAdelantos = adelantosCandidatos
            .GroupBy(ad => (ad.ArtistaId, ad.Moneda))
            .Select(g => new { ArtistaId = g.Key.ArtistaId, g.Key.Moneda, Monto = g.Sum(ad => ad.Tipo == TipoAdelanto.Adelanto ? -ad.Importe : ad.Importe) })
            .ToList();

        decimal Saldo(int artistaId, Moneda moneda) =>
            saldoVentas.Where(s => s.ArtistaId == artistaId && s.Moneda == moneda).Sum(s => s.Monto) +
            saldoAlquileres.Where(s => s.ArtistaId == artistaId && s.Moneda == moneda).Sum(s => s.Monto) +
            saldoAdelantos.Where(s => s.ArtistaId == artistaId && s.Moneda == moneda).Sum(s => s.Monto);

        var resultado = artistas
            .Select(a => new ArtistaListItem(
                a.Id, a.Codigo, a.Nombre, a.Taller, a.Celular, a.Correo,
                a.CantidadObras, a.ObrasEnStock,
                Saldo(a.Id, Moneda.Pesos), Saldo(a.Id, Moneda.USD)))
            .ToList();

        return Ordenar(resultado, filtro);
    }

    // El orden se resuelve en memoria a propósito: la lista completa ya está materializada acá
    // (el saldo se calculó arriba fuera de SQL), y a esta escala ordenar en LINQ-to-Objects es
    // más simple que reproducir el mismo cálculo dentro de la consulta a la base.
    private static List<ArtistaListItem> Ordenar(List<ArtistaListItem> items, ArtistaFiltro filtro)
    {
        IEnumerable<ArtistaListItem> Aplicar<TKey>(Func<ArtistaListItem, TKey> selector) =>
            filtro.OrdenDescendente ? items.OrderByDescending(selector) : items.OrderBy(selector);

        IEnumerable<ArtistaListItem> ordenado = filtro.Orden switch
        {
            OrdenArtista.Codigo => Aplicar(a => a.Codigo),
            OrdenArtista.Taller => Aplicar(a => a.Taller ?? ""),
            OrdenArtista.CantidadObras => Aplicar(a => a.CantidadObras),
            OrdenArtista.ObrasEnStock => Aplicar(a => a.ObrasEnStock),
            OrdenArtista.SaldoPesos => Aplicar(a => a.SaldoPesos),
            OrdenArtista.SaldoDolares => Aplicar(a => a.SaldoDolares),
            _ => Aplicar(a => a.NombreCompleto)
        };

        return ordenado.ToList();
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

    public async Task<ArtistaFicha?> ObtenerFichaAsync(int id, CancellationToken ct = default) =>
        await db.Artistas.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new ArtistaFicha(
                a.Id,
                a.Codigo,
                a.Apellido,
                a.Nombre,
                a.Taller,
                a.Celular,
                a.TelFijo,
                a.Direccion,
                a.Correo,
                a.Perfil,
                a.Obras.Count))
            .FirstOrDefaultAsync(ct);

    // Código queda afuera a propósito (ver ArtistaDtos.cs): no se toca desde acá.
    public async Task ActualizarAsync(ActualizarArtistaRequest request, CancellationToken ct = default)
    {
        var artista = await db.Artistas.FirstAsync(a => a.Id == request.Id, ct);
        artista.Apellido = request.Apellido.Trim();
        artista.Nombre = request.Nombre.Trim();
        artista.Taller = request.Taller;
        artista.Celular = request.Celular;
        artista.TelFijo = request.TelFijo;
        artista.Direccion = request.Direccion;
        artista.Correo = request.Correo;
        artista.Perfil = request.Perfil;
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
