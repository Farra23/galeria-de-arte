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
        //
        // Lo ya liquidado se descarta con un NOT EXISTS contra LineasLiquidacion, no trayendo los
        // ids a memoria: sobre la base real son ~14.000 referencias, y EF las incrustaba como
        // ~14.000 parámetros en un NOT IN que tardaba 1,1 s en cada carga de la lista de artistas.
        // El índice (Tipo, ReferenciaId) de LineaLiquidacionConfiguration es el que sostiene este
        // anti-join; si se lo saca, vuelve a recorrer la tabla entera por cada venta y alquiler.

        // El SUM de SQLite no soporta decimal en la traducción de EF Core — se trae el detalle
        // (sin agregar) y se agrupa/suma en memoria, mismo criterio que ya usa el motor de
        // Liquidaciones (LiquidacionRepository.BuscarPendientesAsync + CalcularTotales).
        var ventasCandidatas = await db.Ventas.AsNoTracking()
            .Where(v => !db.LineasLiquidacion.Any(l =>
                    (l.Tipo == TipoLinea.Venta || l.Tipo == TipoLinea.PagoContado || l.Tipo == TipoLinea.Devolucion)
                    && l.ReferenciaId == v.Id)
                && !v.Obra.PagoContado && v.Devolucion == null)
            .Select(v => new { v.Obra.ArtistaId, v.Moneda, Monto = v.Obra.Costo * v.Cantidad })
            .ToListAsync(ct);

        var alquileresCandidatos = await db.Alquileres.AsNoTracking()
            .Where(a => !db.LineasLiquidacion.Any(l => l.Tipo == TipoLinea.Alquiler && l.ReferenciaId == a.Id))
            .Select(a => new { a.Obra.ArtistaId, a.Moneda, Monto = a.MontoArtista })
            .ToListAsync(ct);

        var adelantosCandidatos = await db.Adelantos.AsNoTracking()
            .Where(ad => ad.LiquidacionId == null)
            .Select(ad => new { ad.ArtistaId, ad.Moneda, ad.Tipo, ad.Importe })
            .ToListAsync(ct);

        // Indexado por (artista, moneda) en vez de recorrer las tres listas por cada artista y
        // moneda: con la lista completa eso eran seis barridos por fila de la grilla.
        var saldoVentas = ventasCandidatas
            .GroupBy(v => (v.ArtistaId, v.Moneda))
            .ToDictionary(g => g.Key, g => g.Sum(v => v.Monto));

        var saldoAlquileres = alquileresCandidatos
            .GroupBy(a => (a.ArtistaId, a.Moneda))
            .ToDictionary(g => g.Key, g => g.Sum(a => a.Monto));

        var saldoAdelantos = adelantosCandidatos
            .GroupBy(ad => (ad.ArtistaId, ad.Moneda))
            .ToDictionary(g => g.Key, g => g.Sum(ad => ad.Tipo == TipoAdelanto.Adelanto ? -ad.Importe : ad.Importe));

        decimal Saldo(int artistaId, Moneda moneda)
        {
            var clave = (artistaId, moneda);
            return saldoVentas.GetValueOrDefault(clave)
                + saldoAlquileres.GetValueOrDefault(clave)
                + saldoAdelantos.GetValueOrDefault(clave);
        }

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
