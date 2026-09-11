using Galeria.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence;

// Funciona porque todos los repositorios reciben por inyección el MISMO GaleriaDbContext (está
// registrado como Scoped): la transacción que se abre acá cubre los SaveChanges de todos ellos.
public class UnitOfWork(GaleriaDbContext db) : IUnitOfWork
{
    public async Task<T> EjecutarEnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken ct = default)
    {
        // Si el llamador ya está dentro de una transacción, no se abre otra (SQLite no anida):
        // se suma a la que está en curso y el commit queda a cargo de quien la abrió.
        if (db.Database.CurrentTransaction is not null)
        {
            return await operacion(ct);
        }

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        var resultado = await operacion(ct);

        await transaccion.CommitAsync(ct);
        return resultado;

        // Si `operacion` lanza, no se llega al Commit y el Dispose del `await using` hace
        // rollback — no hace falta un catch explícito para deshacer.
    }
}
