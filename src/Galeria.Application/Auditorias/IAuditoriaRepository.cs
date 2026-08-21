namespace Galeria.Application.Auditorias;

public interface IAuditoriaRepository
{
    Task RegistrarAsync(Domain.Entities.Auditoria entrada, CancellationToken ct = default);

    Task<List<AuditoriaListItem>> BuscarAsync(AuditoriaFiltro filtro, CancellationToken ct = default);
}
