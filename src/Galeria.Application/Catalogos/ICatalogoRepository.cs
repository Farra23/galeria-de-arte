namespace Galeria.Application.Catalogos;

// Rubros y técnicas: catálogos simples de ABM (sección 14, Configuración). Nunca se borran filas
// (decisión #9 del proyecto) — "baja" es Activo = false, así una obra vieja conserva su rubro.
public interface ICatalogoRepository
{
    Task<List<OpcionCatalogo>> ListarRubrosAsync(CancellationToken ct = default);

    Task<List<OpcionCatalogo>> ListarTecnicasAsync(CancellationToken ct = default);

    Task<List<CatalogoItem>> ListarRubrosAdminAsync(CancellationToken ct = default);

    Task<List<CatalogoItem>> ListarTecnicasAdminAsync(CancellationToken ct = default);

    Task CrearRubroAsync(string nombre, CancellationToken ct = default);

    Task CrearTecnicaAsync(string nombre, CancellationToken ct = default);

    Task ActualizarRubroAsync(int id, string nombre, bool activo, CancellationToken ct = default);

    Task ActualizarTecnicaAsync(int id, string nombre, bool activo, CancellationToken ct = default);
}
