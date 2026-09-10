using Galeria.Domain.Entities;
using Galeria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Galeria.DataImport.Importadores;

/// <summary>
/// Saldos de apertura. Después de importar toda la historia de ventas, el sistema consideraría
/// "pendiente de pagar" cada venta que no tenga una liquidación asociada — es decir, los 17 años.
/// Este importador cierra ese hueco:
///
///  1. Toma de la hoja "Listado Ventas" el saldo real que la galería le debe hoy a cada artista.
///  2. Deja pendientes las ventas más recientes de cada artista hasta llegar a ese monto
///     (el saldo se calcula sobre el <b>costo</b> de la obra, que es lo que cobra el artista).
///  3. Marca todo lo anterior como pagado, con una única "liquidación de apertura" por
///     artista y moneda (Estado = Confirmada, fecha de corte).
///  4. Si queda una diferencia chica contra el objetivo, la salda con un ajuste.
///
/// El historial viejo no queda detallado liquidación por liquidación (decisión tomada con el
/// cliente), pero los saldos arrancan exactos.
/// </summary>
public sealed class AperturaImportador : IImportador
{
    public string Nombre => "Liquidaciones de apertura";

    private const int ColArtista = 2, ColPiezas = 3, ColPesos = 4, ColDolares = 5;
    private const decimal Tolerancia = 0.01m;

    private int _proximoNumero = 1;

    public async Task EjecutarAsync(Fuentes fuentes, Contexto contexto, Informe informe)
    {
        var objetivos = LeerObjetivos(fuentes, contexto, informe);

        var cutoff = await contexto.Db.Ventas.AnyAsync()
            ? await contexto.Db.Ventas.MaxAsync(v => v.Fecha)
            : DateOnly.FromDateTime(DateTime.Today);

        var ventas = await contexto.Db.Ventas
            .Include(v => v.Obra).ThenInclude(o => o.Artista)
            .ToListAsync();

        var adelantosAbiertos = await contexto.Db.Adelantos
            .Where(a => a.LiquidacionId == null)
            .ToListAsync();

        var liquidacionesCreadas = 0;
        var ajustes = 0;

        var porArtista = ventas.GroupBy(v => v.Obra.ArtistaId);
        foreach (var grupoArtista in porArtista)
        {
            var artistaId = grupoArtista.Key;
            var artista = grupoArtista.First().Obra.Artista;

            foreach (var moneda in new[] { Moneda.Pesos, Moneda.USD })
            {
                var delArtista = grupoArtista.Where(v => v.Moneda == moneda)
                    .OrderByDescending(v => v.Fecha).ThenByDescending(v => v.Id)
                    .ToList();
                if (delArtista.Count == 0)
                {
                    continue;
                }

                var objetivo = objetivos.GetValueOrDefault((artistaId, moneda), 0m);
                var (pendientes, pagadas, acumulado) = Repartir(delArtista, objetivo);

                if (pagadas.Count > 0)
                {
                    contexto.Db.Liquidaciones.Add(CrearApertura(artistaId, moneda, cutoff, pagadas));
                    liquidacionesCreadas++;
                }

                // El ajuste solo corrige que a veces no se puede partir una venta para llegar
                // justo al objetivo (la venta más nueva vale más que el saldo pendiente).
                // Nunca se toca el saldo de un artista que no figura en "Listado Ventas".
                var residual = objetivo > 0 ? objetivo - acumulado : 0m;
                if (Math.Abs(residual) >= Tolerancia)
                {
                    contexto.Db.Adelantos.Add(new Adelanto
                    {
                        ArtistaId = artistaId,
                        Fecha = cutoff,
                        Importe = Math.Abs(residual),
                        Moneda = moneda,
                        Tipo = residual > 0 ? TipoAdelanto.AjusteAFavor : TipoAdelanto.Adelanto,
                        Observaciones = "Ajuste de saldo de apertura (migración desde el Excel).",
                    });
                    ajustes++;
                }

                var impactoAdelantos = adelantosAbiertos
                    .Where(a => a.ArtistaId == artistaId && a.Moneda == moneda)
                    .Sum(a => a.ImpactoEnLiquidacion);
                if (objetivo > 0 && impactoAdelantos != 0)
                {
                    informe.Aviso($"{artista.NombreCompleto} ({moneda}): 'Listado Ventas' pide {objetivo:N0} " +
                                  $"y además hay adelantos abiertos por {impactoAdelantos:N2}. El saldo va a quedar en " +
                                  $"{objetivo + impactoAdelantos:N2} — confirmar con el cliente si 'Listado Ventas' ya los tenía en cuenta.");
                }

                if (objetivo > 0 || Math.Abs(residual) >= Tolerancia)
                {
                    informe.Aviso($"Saldo apertura · {artista.NombreCompleto} ({moneda}): " +
                                  $"objetivo {objetivo:N0}, dejado pendiente {acumulado:N0} en {pendientes.Count} ventas, " +
                                  $"pagado {pagadas.Count} ventas, ajuste {residual:N2}");
                }

                await contexto.Db.SaveChangesAsync();
                contexto.Db.ChangeTracker.Clear();
            }
        }

        informe.Importados("Liquidaciones de apertura", liquidacionesCreadas);
        informe.Importados("Ajustes de saldo de apertura", ajustes);
    }

    /// <summary>
    /// Deja pendientes las ventas más nuevas hasta cubrir <paramref name="objetivo"/> (sobre el
    /// costo). Las ventas de pago contado nunca aportan plata: van directo a "pagadas".
    /// </summary>
    private static (List<Venta> Pendientes, List<Venta> Pagadas, decimal Acumulado) Repartir(
        List<Venta> ventasDescendentes, decimal objetivo)
    {
        var pendientes = new List<Venta>();
        var pagadas = new List<Venta>();
        var acumulado = 0m;

        foreach (var venta in ventasDescendentes)
        {
            var aporte = venta.Obra.PagoContado ? 0m : venta.Obra.Costo * venta.Cantidad;
            if (!venta.Obra.PagoContado && acumulado < objetivo)
            {
                pendientes.Add(venta);
                acumulado += aporte;
            }
            else
            {
                pagadas.Add(venta);
            }
        }

        return (pendientes, pagadas, acumulado);
    }

    private Liquidacion CrearApertura(int artistaId, Moneda moneda, DateOnly cutoff, List<Venta> pagadas)
    {
        var lineas = pagadas.Select(v => new LineaLiquidacion
        {
            Tipo = v.Obra.PagoContado ? TipoLinea.PagoContado : TipoLinea.Venta,
            ObraId = v.ObraId,
            Fecha = v.Fecha,
            CodigoObra = v.Obra.Artista.Codigo.ToString("D3") + v.Obra.NumeroObra.ToString("D3"),
            NombreObra = Recortar(v.Obra.Titulo, 200),
            Cantidad = v.Cantidad,
            MontoUnitario = v.Obra.PagoContado ? 0 : v.Obra.Costo,
            MontoTotal = v.Obra.PagoContado ? 0 : v.Obra.Costo * v.Cantidad,
            Observaciones = "Liquidación de apertura (migración)",
            ReferenciaId = v.Id,
        }).ToList();

        var total = lineas.Sum(l => l.MontoTotal);

        return new Liquidacion
        {
            ArtistaId = artistaId,
            NumeroCorrelativo = _proximoNumero++,
            Fecha = cutoff,
            PeriodoAnio = cutoff.Year,
            PeriodoMes = cutoff.Month,
            Moneda = moneda,
            Estado = EstadoLiquidacion.Confirmada,
            TotalBruto = total,
            TotalAdelantos = 0,
            TotalDevoluciones = 0,
            TotalNeto = total,
            Lineas = lineas,
        };
    }

    private static Dictionary<(int Artista, Moneda Moneda), decimal> LeerObjetivos(
        Fuentes fuentes, Contexto contexto, Informe informe)
    {
        var objetivos = new Dictionary<(int, Moneda), decimal>();
        if (fuentes.Liquidaciones is null)
        {
            informe.Aviso("No se encontró Liquidaciones.xlsm — todos los saldos de apertura arrancan en 0.");
            return objetivos;
        }

        foreach (var fila in fuentes.Liquidaciones.Filas("Listado Ventas", filasEncabezado: 2))
        {
            var nombre = fila.Texto(ColArtista);
            if (nombre is null || Texto.Clave(nombre) is "artista")
            {
                continue;
            }

            var artistaId = contexto.ResolverArtista(null, nombre);
            if (artistaId is null)
            {
                informe.Rechazo("Saldos de apertura", $"artista de 'Listado Ventas' no encontrado: {nombre}");
                continue;
            }

            var pesos = fila.Decimal(ColPesos);
            var dolares = fila.Decimal(ColDolares);
            if (pesos is > 0)
            {
                objetivos[(artistaId.Value, Moneda.Pesos)] = pesos.Value;
            }

            if (dolares is > 0)
            {
                objetivos[(artistaId.Value, Moneda.USD)] = dolares.Value;
            }
        }

        return objetivos;
    }

    private static string Recortar(string valor, int max) => valor.Length > max ? valor[..max] : valor;
}
