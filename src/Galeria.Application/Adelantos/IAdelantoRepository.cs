namespace Galeria.Application.Adelantos;

public interface IAdelantoRepository
{
    Task AgregarAsync(Domain.Entities.Adelanto adelanto, CancellationToken ct = default);

    Task<List<AdelantoListItem>> BuscarAsync(AdelantoFiltro filtro, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
