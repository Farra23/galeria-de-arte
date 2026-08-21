using Galeria.Application.Auditorias;
using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence.Repositories;

public class AuditoriaRepository(GaleriaDbContext db) : IAuditoriaRepository
{
    // Sin SaveChanges acá a propósito: la auditoría siempre se escribe como parte de una
    // operación más grande (registrar una venta, un retiro...) que guarda todo junto en una
    // sola transacción — mismo patrón de unit-of-work que ObraRepository.AgregarAsync.
    public Task RegistrarAsync(Auditoria entrada, CancellationToken ct = default)
    {
        db.Auditorias.Add(entrada);
        return Task.CompletedTask;
    }

    public async Task<List<AuditoriaListItem>> BuscarAsync(AuditoriaFiltro filtro, CancellationToken ct = default)
    {
        var query = db.Auditorias.AsNoTracking().AsQueryable();

        if (filtro.FechaDesde is not null)
        {
            var desde = filtro.FechaDesde.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(a => a.Timestamp >= desde);
        }

        if (filtro.FechaHasta is not null)
        {
            var hasta = filtro.FechaHasta.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(a => a.Timestamp <= hasta);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Usuario))
        {
            var usuario = filtro.Usuario.Trim();
            query = query.Where(a => EF.Functions.Like(a.NombreUsuario, $"%{usuario}%"));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Pantalla))
        {
            query = query.Where(a => a.Pantalla == filtro.Pantalla);
        }

        if (!string.IsNullOrWhiteSpace(filtro.TipoOperacion))
        {
            query = query.Where(a => a.TipoOperacion == filtro.TipoOperacion);
        }

        if (filtro.ObraId is not null)
        {
            query = query.Where(a => a.ObraId == filtro.ObraId);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Columna))
        {
            query = query.Where(a => a.Columna == filtro.Columna);
        }

        // SQLite no soporta ORDER BY sobre DateTimeOffset (traducción no soportada por el
        // proveedor) — se ordena por Id, que al ser autonumérico va en el mismo orden cronológico.
        return await query
            .OrderByDescending(a => a.Id)
            .Take(500)
            .Select(a => new AuditoriaListItem(
                a.Id,
                a.Timestamp,
                a.NombreUsuario,
                a.Pantalla,
                a.TipoOperacion,
                a.Tabla,
                a.Columna,
                a.ValorAnterior,
                a.ValorNuevo,
                a.ArtistaId,
                a.ObraId,
                a.EntidadId))
            .ToListAsync(ct);
    }
}
