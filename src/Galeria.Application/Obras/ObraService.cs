using Galeria.Application.Auditorias;
using Galeria.Application.Parametros;
using Galeria.Domain.Entities;
using Galeria.Domain.Enums;

namespace Galeria.Application.Obras;

public class ObraService(IObraRepository obras, IParametroRepository parametros, AuditoriaService auditoria)
{
    public Task<List<ObraListItem>> BuscarAsync(ObraFiltro filtro, CancellationToken ct = default) =>
        obras.BuscarAsync(filtro, ct);

    public Task<List<SerieOpcion>> ListarSeriesAsync(CancellationToken ct = default) =>
        obras.ListarSeriesAsync(ct);

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
        var obra = ConstruirObra(request, numeroObra, serieId: null);

        await obras.AgregarAsync(obra, ct);
        await obras.GuardarCambiosAsync(ct);

        await auditoria.RegistrarAsync(new RegistrarAuditoriaRequest(
            Pantalla: "Obras",
            TipoOperacion: "Alta",
            Tabla: "Obra",
            ValorNuevo: obra.Titulo,
            ArtistaId: obra.ArtistaId,
            ObraId: obra.Id,
            EntidadId: obra.Id.ToString()), ct);

        return obra.Id;
    }

    // Serie (requerimiento 3.3): "10 caravanas distintas" → un solo formulario, N obras con
    // códigos consecutivos, todas marcadas con la misma Serie (campo aparte, no un sufijo del
    // código). Si cantidadPiezas es 1 no tiene sentido crear una Serie de una sola pieza — para
    // ese caso la Web ya usa CrearAsync directamente.
    public async Task<int> CrearSerieAsync(CrearObraRequest request, int cantidadPiezas, string nombreSerie, CancellationToken ct = default)
    {
        var serie = new Serie { ArtistaId = request.ArtistaId, Nombre = nombreSerie.Trim() };
        await obras.AgregarSerieAsync(serie, ct);
        await obras.GuardarCambiosAsync(ct); // necesito serie.Id antes de crear las obras

        var numeroInicial = await obras.ProximoNumeroObraAsync(request.ArtistaId, ct);
        var creadas = new List<Obra>();

        for (var i = 0; i < cantidadPiezas; i++)
        {
            var obra = ConstruirObra(request, numeroInicial + i, serie.Id);
            obra.Titulo = $"{obra.Titulo} {NumeroRomano(i + 1)}";
            await obras.AgregarAsync(obra, ct);
            creadas.Add(obra);
        }

        await obras.GuardarCambiosAsync(ct);

        foreach (var obra in creadas)
        {
            await auditoria.RegistrarAsync(new RegistrarAuditoriaRequest(
                Pantalla: "Obras",
                TipoOperacion: "Alta",
                Tabla: "Obra",
                Columna: "Serie",
                ValorNuevo: obra.Titulo,
                ArtistaId: obra.ArtistaId,
                ObraId: obra.Id,
                EntidadId: obra.Id.ToString()), ct);
        }

        return serie.Id;
    }

    // Requerimiento 3.3 "+": cada pieza de una serie se distingue en el nombre con un numeral
    // romano (I, II, III...) además del código correlativo que ya la distingue en la base.
    private static string NumeroRomano(int numero)
    {
        var valores = new[] { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
        var simbolos = new[] { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };

        var resultado = new System.Text.StringBuilder();
        for (var i = 0; i < valores.Length && numero > 0; i++)
        {
            while (numero >= valores[i])
            {
                resultado.Append(simbolos[i]);
                numero -= valores[i];
            }
        }

        return resultado.ToString();
    }

    private static Obra ConstruirObra(CrearObraRequest request, int numeroObra, int? serieId) => new()
    {
        ArtistaId = request.ArtistaId,
        NumeroObra = numeroObra,
        SerieId = serieId,
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

    public async Task AgregarExistenciaAsync(int obraId, int cantidad, CancellationToken ct = default)
    {
        if (cantidad < 1 || cantidad > 9999)
        {
            throw new InvalidOperationException("La cantidad a agregar tiene que estar entre 1 y 9999.");
        }

        await obras.AumentarExistenciaAsync(obraId, cantidad, ct);
        await obras.GuardarCambiosAsync(ct);
    }

    public Task<ObraFicha?> ObtenerFichaAsync(int id, CancellationToken ct = default) =>
        obras.ObtenerFichaAsync(id, ct);

    // Historial de precios (requerimiento 3.1, pestaña de la ficha): en vez de una tabla nueva,
    // cada cambio de precio se registra en Auditoría — es la misma fuente que el requerimiento 13
    // ya pide como "única fuente para reconstruir cómo se llegó al stock actual".
    public async Task ActualizarAsync(ActualizarObraRequest request, CancellationToken ct = default)
    {
        var anterior = await obras.ObtenerFichaAsync(request.Id, ct);

        await obras.ActualizarAsync(request, ct);
        await RegistrarCambioPrecioSiCorrespondeAsync(anterior, request.Id, request.PrecioVenta, ct);
        await obras.GuardarCambiosAsync(ct);
    }

    // Cambio de precio unitario o global (requerimiento 12): a diferencia de ActualizarAsync no
    // toca el resto de la ficha — pensado para el flujo de Cambio de precios, que puede tocar
    // muchas obras de un mismo artista en una sola operación.
    public async Task ActualizarPrecioAsync(int obraId, decimal nuevoPrecio, CancellationToken ct = default)
    {
        if (nuevoPrecio <= 0 || nuevoPrecio > 999999999)
        {
            throw new InvalidOperationException("El precio nuevo tiene que ser mayor a 0 y no superar 999.999.999.");
        }

        var anterior = await obras.ObtenerFichaAsync(obraId, ct);

        await obras.ActualizarPrecioAsync(obraId, nuevoPrecio, ct);
        await RegistrarCambioPrecioSiCorrespondeAsync(anterior, obraId, nuevoPrecio, ct);
        await obras.GuardarCambiosAsync(ct);
    }

    private async Task RegistrarCambioPrecioSiCorrespondeAsync(ObraFicha? anterior, int obraId, decimal precioNuevo, CancellationToken ct)
    {
        if (anterior is null || anterior.PrecioVenta == precioNuevo)
        {
            return;
        }

        await auditoria.RegistrarAsync(new RegistrarAuditoriaRequest(
            Pantalla: "Obras",
            TipoOperacion: "Modificacion",
            Tabla: "Obra",
            Columna: "PrecioVenta",
            ValorAnterior: anterior.PrecioVenta.ToString("0.##"),
            ValorNuevo: precioNuevo.ToString("0.##"),
            ArtistaId: anterior.ArtistaId,
            ObraId: obraId,
            EntidadId: obraId.ToString()), ct);
    }

    // Requerimiento 3.2: "se puede subir al cargar la obra o más tarde" — el Web ya guardó el
    // archivo en wwwroot antes de llamar acá, este método solo actualiza la referencia y audita.
    public async Task ActualizarImagenAsync(int obraId, string? rutaRelativa, CancellationToken ct = default)
    {
        var ficha = await obras.ObtenerFichaAsync(obraId, ct);

        await obras.ActualizarImagenAsync(obraId, rutaRelativa, ct);
        await obras.GuardarCambiosAsync(ct);

        if (ficha is not null)
        {
            await auditoria.RegistrarAsync(new RegistrarAuditoriaRequest(
                Pantalla: "Obras",
                TipoOperacion: "Modificacion",
                Tabla: "Obra",
                Columna: "Imagen",
                ValorAnterior: ficha.ImagenUrl,
                ValorNuevo: rutaRelativa,
                ArtistaId: ficha.ArtistaId,
                ObraId: obraId,
                EntidadId: obraId.ToString()), ct);
        }
    }

    // Etiquetas adhesivas con código de barras: se marca al momento de generar el PDF (mismo
    // criterio que "Descargar PDF" en Certificado/Liquidación, no hay forma de confirmar que la
    // impresión física salió bien, así que "se generó el PDF" es la señal disponible).
    public async Task MarcarEtiquetaImpresaAsync(int obraId, CancellationToken ct = default)
    {
        var ficha = await obras.ObtenerFichaAsync(obraId, ct);

        await obras.MarcarEtiquetaImpresaAsync(obraId, ct);
        await obras.GuardarCambiosAsync(ct);

        if (ficha is not null)
        {
            await auditoria.RegistrarAsync(new RegistrarAuditoriaRequest(
                Pantalla: "Obras",
                TipoOperacion: "Modificacion",
                Tabla: "Obra",
                Columna: "EtiquetaImpresa",
                ValorAnterior: ficha.FechaEtiquetaImpresa?.ToString("dd/MM/yyyy"),
                ValorNuevo: DateOnly.FromDateTime(DateTime.Now).ToString("dd/MM/yyyy"),
                ArtistaId: ficha.ArtistaId,
                ObraId: obraId,
                EntidadId: obraId.ToString()), ct);
        }
    }

    public Task<List<MovimientoItem>> ObtenerMovimientosAsync(int obraId, CancellationToken ct = default) =>
        obras.ObtenerMovimientosAsync(obraId, ct);

    public Task<List<Auditorias.AuditoriaListItem>> ObtenerHistorialPreciosAsync(int obraId, CancellationToken ct = default) =>
        auditoria.BuscarAsync(new Auditorias.AuditoriaFiltro(ObraId: obraId, Columna: "PrecioVenta"), ct);
}
