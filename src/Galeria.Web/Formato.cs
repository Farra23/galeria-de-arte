using Galeria.Domain.Enums;

namespace Galeria.Web;

// Requerimiento 0.7/0.8 (docs/REQUERIMIENTOS): moneda siempre con símbolo explícito,
// pesos y dólares nunca se mezclan. Un solo lugar para no repetir el switch en cada página.
public static class Formato
{
    public static string Simbolo(Moneda moneda) => moneda == Moneda.Pesos ? "$" : "U$S";

    public static string Monto(decimal valor, Moneda moneda) => $"{Simbolo(moneda)} {valor:N2}";

    public static string Estado(EstadoObra estado) => estado switch
    {
        EstadoObra.Disponible => "Disponible",
        EstadoObra.RetiradaTemporal => "Retirada temporal",
        EstadoObra.Alquilada => "Alquilada",
        EstadoObra.RetiradaDefinitiva => "Retirada definitiva",
        EstadoObra.SinStock => "Sin stock",
        _ => estado.ToString()
    };
}
