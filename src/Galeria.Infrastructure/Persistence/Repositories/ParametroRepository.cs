using Galeria.Application.Parametros;
using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence.Repositories;

public class ParametroRepository(GaleriaDbContext db) : IParametroRepository
{
    public async Task<string?> ObtenerAsync(string clave, CancellationToken ct = default) =>
        await db.Parametros.AsNoTracking()
            .Where(p => p.Clave == clave)
            .Select(p => p.Valor)
            .FirstOrDefaultAsync(ct);

    public async Task<Dictionary<string, string>> ObtenerVariosAsync(IEnumerable<string> claves, CancellationToken ct = default)
    {
        var listaClaves = claves.ToList();
        return await db.Parametros.AsNoTracking()
            .Where(p => listaClaves.Contains(p.Clave))
            .ToDictionaryAsync(p => p.Clave, p => p.Valor, ct);
    }

    public async Task GuardarAsync(string clave, string? valor, CancellationToken ct = default)
    {
        var parametro = await db.Parametros.FirstOrDefaultAsync(p => p.Clave == clave, ct);

        if (parametro is null)
        {
            db.Parametros.Add(new Parametro { Clave = clave, Valor = valor ?? "" });
        }
        else
        {
            parametro.Valor = valor ?? "";
        }

        await db.SaveChangesAsync(ct);
    }
}
