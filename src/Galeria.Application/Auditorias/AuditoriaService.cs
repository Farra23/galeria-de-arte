using Galeria.Application.Common;
using Galeria.Domain.Entities;

namespace Galeria.Application.Auditorias;

// Punto único de escritura de Auditoría (requerimiento 13: "automática y de TODO"). Cada
// servicio de aplicación que hace un alta/modificación/baja relevante llama acá en vez de
// escribir el registro a mano — un solo lugar decide cómo se arma una fila de Auditoria.
public class AuditoriaService(IAuditoriaRepository repositorio, ICurrentUserService usuarioActual)
{
    public async Task RegistrarAsync(RegistrarAuditoriaRequest request, CancellationToken ct = default)
    {
        var entrada = new Auditoria
        {
            Timestamp = DateTimeOffset.Now,
            UsuarioId = await usuarioActual.ObtenerUsuarioIdAsync(),
            NombreUsuario = await usuarioActual.ObtenerNombreUsuarioAsync(),
            Pantalla = request.Pantalla,
            TipoOperacion = request.TipoOperacion,
            Tabla = request.Tabla,
            Columna = request.Columna,
            ValorAnterior = request.ValorAnterior,
            ValorNuevo = request.ValorNuevo,
            ArtistaId = request.ArtistaId,
            ObraId = request.ObraId,
            EntidadId = request.EntidadId
        };

        await repositorio.RegistrarAsync(entrada, ct);
    }

    public Task<List<AuditoriaListItem>> BuscarAsync(AuditoriaFiltro filtro, CancellationToken ct = default) =>
        repositorio.BuscarAsync(filtro, ct);
}
