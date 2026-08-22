using Galeria.Application.Auditorias;
using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence.Repositories;

public class AuditoriaRepository(GaleriaDbContext db) : IAuditoriaRepository
{
    // Guarda inmediatamente: a diferencia del resto de los repositorios (donde el llamador
    // controla cuándo hacer SaveChanges porque hay más entidades tocadas en la misma operación),
    // Auditoría no puede depender de que "alguna otra parte de la operación guarde" — la pantalla
    // de Usuarios (ABM de Identity, requerimiento 14) no toca ningún otro repositorio de
    // GaleriaDbContext, y con el diseño anterior el registro de auditoría se perdía en silencio.
    // Guardar acá no rompe nada en los flujos que sí acumulan varias entidades: si ya había un
    // Movimiento u otra entidad pendiente en el mismo DbContext, este SaveChanges los confirma
    // igual — solo adelanta el punto de guardado, no lo duplica ni lo pisa.
    public async Task RegistrarAsync(Auditoria entrada, CancellationToken ct = default)
    {
        db.Auditorias.Add(entrada);
        await db.SaveChangesAsync(ct);
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
