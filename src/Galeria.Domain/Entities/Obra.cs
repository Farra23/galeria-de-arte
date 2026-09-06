namespace Galeria.Domain.Entities;

public class Obra
{
    public int Id { get; set; }
    public int ArtistaId { get; set; }
    public int NumeroObra { get; set; }    // correlativo por artista; puede superar 999
    public int? SerieId { get; set; }
    public int? RubroId { get; set; }
    public int? TecnicaId { get; set; }

    public string Titulo { get; set; } = string.Empty;
    public Enums.Moneda Moneda { get; set; }
    public decimal Costo { get; set; }
    public decimal Utilidad { get; set; } = 50;  // porcentaje, default 50 %
    public bool TieneIVA { get; set; }
    public decimal PrecioVenta { get; set; }      // almacenado y editable
    public int Existencia { get; set; }
    public bool PagoContado { get; set; }
    public DateOnly FechaIngreso { get; set; }
    public Enums.EstadoObra Estado { get; set; } = Enums.EstadoObra.Disponible;

    public decimal? AltoCm { get; set; }
    public decimal? AnchoCm { get; set; }
    public decimal? LargoCm { get; set; }
    public string? Observaciones { get; set; }
    public string? ImagenPrincipalPath { get; set; }
    public DateOnly? FechaEtiquetaImpresa { get; set; }

    public Artista Artista { get; set; } = null!;
    public Serie? Serie { get; set; }
    public Rubro? Rubro { get; set; }
    public Tecnica? Tecnica { get; set; }
    public ICollection<Venta> Ventas { get; set; } = [];
    public ICollection<Retiro> Retiros { get; set; } = [];
    public ICollection<Alquiler> Alquileres { get; set; } = [];
    public ICollection<Movimiento> Movimientos { get; set; } = [];

    // GRASP Information Expert: la obra calcula su propio precio según las reglas del negocio.
    // El llamador inyecta IVA y redondeo reales desde Parametro para no acoplar Domain a la BD.
    public decimal CalcularPrecioVenta(decimal ivaPorcentaje = 22m, decimal redondeo = 1m) =>
        CalcularPrecioVenta(Costo, Utilidad, TieneIVA, ivaPorcentaje, redondeo);

    // Versión estática de la misma fórmula: la usa la pantalla de Alta, donde todavía no existe
    // una Obra (solo valores sueltos del formulario). Una sola fuente de verdad para el cálculo,
    // la instancia de arriba es un atajo cuando la obra ya existe.
    public static decimal CalcularPrecioVenta(decimal costo, decimal utilidad, bool tieneIva, decimal ivaPorcentaje = 22m, decimal redondeo = 1m)
    {
        var factorIva = tieneIva ? 1 + ivaPorcentaje / 100 : 1m;
        var precioBruto = costo * factorIva * (1 + utilidad / 100);
        return redondeo <= 0
            ? precioBruto
            : Math.Round(precioBruto / redondeo, MidpointRounding.AwayFromZero) * redondeo;
    }

    // Código visible de 6 dígitos (puede ser mayor si NumeroObra > 999)
    public string CodigoVisible =>
        Artista != null ? $"{Artista.Codigo:D3}{NumeroObra:D3}" : $"???{NumeroObra:D3}";

    public bool EstaDisponible =>
        Estado == Enums.EstadoObra.Disponible && Existencia > 0;
}