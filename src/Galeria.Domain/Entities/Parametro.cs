namespace Galeria.Domain.Entities;

public class Parametro
{
    public string Clave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;

    // Claves conocidas del sistema — evita magic strings en todo el codebase
    public static class Claves
    {
        public const string IvaPorcentaje    = "IvaPorcentaje";
        public const string RedondeosPesos   = "RedondeosPesos";
        public const string RedondeoDolar    = "RedondeoDolar";
        public const string UtilidadDefault  = "UtilidadDefault";
        public const string NombreGaleria    = "NombreGaleria";
        public const string DireccionGaleria = "DireccionGaleria";
        public const string TelefonoGaleria  = "TelefonoGaleria";
        public const string LogoPath         = "LogoPath";
        public const string PlantillaAviso   = "PlantillaAviso";
        public const string PlantillaLiqui   = "PlantillaLiqui";
        public const string PlantillaCertif  = "PlantillaCertif";
    }
}