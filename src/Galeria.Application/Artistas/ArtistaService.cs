using Galeria.Application.Auditorias;
using Galeria.Domain.Entities;

namespace Galeria.Application.Artistas;

public class ArtistaService(IArtistaRepository repositorio, AuditoriaService auditoria)
{
    public Task<List<ArtistaListItem>> BuscarAsync(ArtistaFiltro filtro, CancellationToken ct = default) =>
        repositorio.BuscarAsync(filtro, ct);

    public Task<List<ArtistaOpcion>> ListarParaSeleccionAsync(CancellationToken ct = default) =>
        repositorio.ListarActivosAsync(ct);

    public async Task<int> CrearAsync(CrearArtistaRequest request, CancellationToken ct = default)
    {
        // GRASP Creator: quien mejor sabe cuál es el próximo código libre es el repositorio
        // (es el único que puede consultar la tabla), el servicio solo orquesta.
        var codigo = await repositorio.ProximoCodigoAsync(ct);

        var artista = new Artista
        {
            Codigo = codigo,
            Apellido = request.Apellido.Trim(),
            Nombre = request.Nombre.Trim(),
            Taller = request.Taller,
            Celular = request.Celular,
            TelFijo = request.TelFijo,
            Direccion = request.Direccion,
            Correo = request.Correo,
            Perfil = request.Perfil
        };

        await repositorio.AgregarAsync(artista, ct);
        await repositorio.GuardarCambiosAsync(ct);

        await auditoria.RegistrarAsync(new RegistrarAuditoriaRequest(
            Pantalla: "Artistas",
            TipoOperacion: "Alta",
            Tabla: "Artista",
            ValorNuevo: $"{artista.Apellido}, {artista.Nombre}",
            ArtistaId: artista.Id,
            EntidadId: artista.Id.ToString()), ct);

        return artista.Id;
    }

    public Task<ArtistaFicha?> ObtenerFichaAsync(int id, CancellationToken ct = default) =>
        repositorio.ObtenerFichaAsync(id, ct);

    public async Task ActualizarAsync(ActualizarArtistaRequest request, CancellationToken ct = default)
    {
        await repositorio.ActualizarAsync(request, ct);
        await repositorio.GuardarCambiosAsync(ct);
    }
}
