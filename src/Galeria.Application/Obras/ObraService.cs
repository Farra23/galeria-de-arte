using Galeria.Application.Parametros;
using Galeria.Domain.Entities;
using Galeria.Domain.Enums;

namespace Galeria.Application.Obras;

public class ObraService(IObraRepository obras, IParametroRepository parametros)
{
    public Task<List<ObraListItem>> BuscarAsync(ObraFiltro filtro, CancellationToken ct = default) =>
        obras.BuscarAsync(filtro, ct);

    public Task<ObraCoincidente?> BuscarCoincidenciaAsync(int artistaId, string titulo, CancellationToken ct = default) =>
        string.IsNullOrWhiteSpace(titulo)
            ? Task.FromResult<ObraCoincidente?>(null)
            : obras.BuscarCoincidenciaAsync(artistaId, titulo.Trim(), ct);

    // Se leen una sola vez al entrar al formulario de Alta — evita ir a la base en cada tecla
    // que tipea el usuario en Costo/Utilidad.
    public async Task<ParametrosCalculoPrecio> ObtenerParametrosCalculoAsync(CancellationToken ct = default)
    {
        var ivaTexto = await parametros.ObtenerAsync(Parametro.Claves.IvaPorcentaje, ct);
        var utilidadTexto = await parametros.ObtenerAsync(Parametro.Claves.UtilidadDefault, ct);
        var redondeoPesosTexto = await parametros.ObtenerAsync(Parametro.Claves.RedondeosPesos, ct);
        var redondeoDolarTexto = await parametros.ObtenerAsync(Parametro.Claves.RedondeoDolar, ct);

        var iva = decimal.TryParse(ivaTexto, out var ivaValor) ? ivaValor : 22m;
        var utilidad = decimal.TryParse(utilidadTexto, out var utilidadValor) ? utilidadValor : 50m;
        var redondeoPesos = decimal.TryParse(redondeoPesosTexto, out var redondeoPesosValor) ? redondeoPesosValor : 10m;
        var redondeoDolar = decimal.TryParse(redondeoDolarTexto, out var redondeoDolarValor) ? redondeoDolarValor : 1m;

        return new ParametrosCalculoPrecio(iva, utilidad, redondeoPesos, redondeoDolar);
    }

    public async Task<int> CrearAsync(CrearObraRequest request, CancellationToken ct = default)
    {
        var numeroObra = await obras.ProximoNumeroObraAsync(request.ArtistaId, ct);

        var obra = new Obra
        {
            ArtistaId = request.ArtistaId,
            NumeroObra = numeroObra,
            Titulo = request.Titulo.Trim(),
            RubroId = request.RubroId,
            TecnicaId = request.TecnicaId,
            AltoCm = request.AltoCm,
            AnchoCm = request.AnchoCm,
            LargoCm = request.LargoCm,
            Existencia = request.Existencia,
            Moneda = request.Moneda,
            Costo = request.Costo,
            Utilidad = request.Utilidad,
            TieneIVA = request.TieneIVA,
            PrecioVenta = request.PrecioVenta,
            PagoContado = request.PagoContado,
            FechaIngreso = DateOnly.FromDateTime(DateTime.Now),
            Observaciones = request.Observaciones,
            Estado = EstadoObra.Disponible
        };

        await obras.AgregarAsync(obra, ct);
        await obras.GuardarCambiosAsync(ct);

        return obra.Id;
    }

    public async Task AgregarExistenciaAsync(int obraId, int cantidad, CancellationToken ct = default)
    {
        await obras.AumentarExistenciaAsync(obraId, cantidad, ct);
        await obras.GuardarCambiosAsync(ct);
    }

    public Task<ObraFicha?> ObtenerFichaAsync(int id, CancellationToken ct = default) =>
        obras.ObtenerFichaAsync(id, ct);

    public async Task ActualizarAsync(ActualizarObraRequest request, CancellationToken ct = default)
    {
        await obras.ActualizarAsync(request, ct);
        await obras.GuardarCambiosAsync(ct);
    }
}
