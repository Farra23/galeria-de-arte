using Galeria.Application.Parametros;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence.Repositories;

public class ParametroRepository(GaleriaDbContext db) : IParametroRepository
{
    public async Task<string?> ObtenerAsync(string clave, CancellationToken ct = default) =>
        await db.Parametros.AsNoTracking()
            .Where(p => p.Clave == clave)
            .Select(p => p.Valor)
            .FirstOrDefaultAsync(ct);
}
