namespace Galeria.Application.Agenda;

public interface IAgendaPagoRepository
{
    // Diccionario indexado por ArtistaId: la Agenda siempre se lee para todos los artistas con
    // saldo pendiente a la vez, nunca de a uno.
    Task<Dictionary<int, Domain.Entities.AgendaPago>> ObtenerTodasAsync(CancellationToken ct = default);

    Task GuardarAsync(GuardarAgendaRequest request, CancellationToken ct = default);
}
