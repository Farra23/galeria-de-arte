using Galeria.Domain.Entities;
using Galeria.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Galeria.DataImport;

/// <summary>
/// Estado compartido entre importadores: la conexión a la base y los mapas para resolver
/// referencias entre planillas (una venta apunta a una obra por código de artista + código de obra,
/// no por Id).
/// </summary>
public sealed class Contexto(GaleriaDbContext db)
{
    public GaleriaDbContext Db { get; } = db;

    /// <summary>Código de artista (3 dígitos) → Id interno.</summary>
    public Dictionary<int, int> ArtistaPorCodigo { get; } = [];

    /// <summary>Clave de nombre normalizada ("apellido nombre") → Id interno. Para cotejar filas que traen el nombre y no el código.</summary>
    public Dictionary<string, int> ArtistaPorNombre { get; } = [];

    /// <summary>(código de artista, número de obra) → Id interno de la obra.</summary>
    public Dictionary<(int Artista, int Obra), int> ObraPorCodigo { get; } = [];

    public async Task RecargarMapasAsync()
    {
        ArtistaPorCodigo.Clear();
        ArtistaPorNombre.Clear();
        ObraPorCodigo.Clear();

        var artistas = await Db.Artistas.AsNoTracking()
            .Select(a => new { a.Id, a.Codigo, a.Apellido, a.Nombre })
            .ToListAsync();

        foreach (var a in artistas)
        {
            ArtistaPorCodigo[a.Codigo] = a.Id;
            ArtistaPorNombre.TryAdd(Texto.Clave($"{a.Apellido} {a.Nombre}"), a.Id);
            ArtistaPorNombre.TryAdd(Texto.Clave($"{a.Nombre} {a.Apellido}"), a.Id);
        }

        var obras = await Db.Obras.AsNoTracking()
            .Select(o => new { o.Id, Codigo = o.Artista.Codigo, o.NumeroObra })
            .ToListAsync();

        foreach (var o in obras)
        {
            ObraPorCodigo[(o.Codigo, o.NumeroObra)] = o.Id;
        }
    }

    public int? ResolverArtista(int? codigo, string? nombre)
    {
        if (codigo is not null && ArtistaPorCodigo.TryGetValue(codigo.Value, out var porCodigo))
        {
            return porCodigo;
        }

        if (nombre is not null && ArtistaPorNombre.TryGetValue(Texto.Clave(nombre), out var porNombre))
        {
            return porNombre;
        }

        return null;
    }
}
