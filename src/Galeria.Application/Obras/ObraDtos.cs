using Galeria.Domain.Enums;

namespace Galeria.Application.Obras;

public record ObraListItem(
    int Id,
    string CodigoVisible,
    string Titulo,
    string ArtistaNombre,
    string? Rubro,
    string? Tecnica,
    Moneda Moneda,
    decimal Costo,
    decimal PrecioVenta,
    int Existencia,
    EstadoObra Estado,
    DateOnly FechaIngreso);

// Lo que se ofrece cuando el nombre tipeado coincide con una obra ya cargada al mismo artista
// (decisión #6, docs/CONTEXTO.md): en vez de crear una obra nueva, se suma existencia a esta.
public record ObraCoincidente(int Id, string CodigoVisible, string Titulo, int Existencia, decimal Costo);

public record CrearObraRequest(
    int ArtistaId,
    string Titulo,
    int? RubroId,
    int? TecnicaId,
    decimal? AltoCm,
    decimal? AnchoCm,
    decimal? LargoCm,
    int Existencia,
    Moneda Moneda,
    decimal Costo,
    decimal Utilidad,
    bool TieneIVA,
    decimal PrecioVenta,
    bool PagoContado,
    string? Observaciones);

public record ParametrosCalculoPrecio(decimal IvaPorcentaje, decimal UtilidadDefault, decimal RedondeoPesos, decimal RedondeoDolar)
{
    public decimal RedondeoPara(Moneda moneda) => moneda == Moneda.Pesos ? RedondeoPesos : RedondeoDolar;
}
