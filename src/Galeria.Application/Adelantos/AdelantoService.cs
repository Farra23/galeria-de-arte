using Galeria.Application.Auditorias;
using Galeria.Domain.Entities;

namespace Galeria.Application.Adelantos;

public class AdelantoService(IAdelantoRepository adelantos, AuditoriaService auditoria)
{
    public Task<List<AdelantoListItem>> BuscarAsync(AdelantoFiltro filtro, CancellationToken ct = default) =>
        adelantos.BuscarAsync(filtro, ct);

    public async Task<int> RegistrarAsync(RegistrarAdelantoRequest request, CancellationToken ct = default)
    {
        if (request.Importe <= 0)
        {
            throw new InvalidOperationException("El importe tiene que ser mayor a 0.");
        }

        var adelanto = new Adelanto
        {
            ArtistaId = request.ArtistaId,
            Fecha = request.Fecha,
            Importe = request.Importe,
            Moneda = request.Moneda,
            Tipo = request.Tipo,
            Observaciones = request.Observaciones
        };

        await adelantos.AgregarAsync(adelanto, ct);

        await auditoria.RegistrarAsync(new RegistrarAuditoriaRequest(
            Pantalla: "Adelantos",
            TipoOperacion: "Alta",
            Tabla: "Adelanto",
            ArtistaId: request.ArtistaId,
            ValorNuevo: $"{request.Tipo} {request.Importe}"), ct);

        await adelantos.GuardarCambiosAsync(ct);

        return adelanto.Id;
    }
}
