using Galeria.Application.Agenda;
using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence.Repositories;

public class AgendaPagoRepository(GaleriaDbContext db) : IAgendaPagoRepository
{
    public async Task<Dictionary<int, AgendaPago>> ObtenerTodasAsync(CancellationToken ct = default) =>
        await db.AgendasPago.AsNoTracking().ToDictionaryAsync(a => a.ArtistaId, ct);

    public async Task GuardarAsync(GuardarAgendaRequest request, CancellationToken ct = default)
    {
        var agenda = await db.AgendasPago.FirstOrDefaultAsync(a => a.ArtistaId == request.ArtistaId, ct);

        if (agenda is null)
        {
            agenda = new AgendaPago { ArtistaId = request.ArtistaId };
            db.AgendasPago.Add(agenda);
        }

        agenda.FechaPactada = request.FechaPactada;
        agenda.FechaConfirmada = request.FechaConfirmada;
        agenda.Comentarios = request.Comentarios;

        await db.SaveChangesAsync(ct);
    }
}
