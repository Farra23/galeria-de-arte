namespace Galeria.Application.Catalogos;

// Rubros y técnicas todavía no tienen ABM propia (sección 14, Configuración — queda para más
// adelante); esto solo lee lo que exista para poblar los desplegables del alta de Obra.
public interface ICatalogoRepository
{
    Task<List<OpcionCatalogo>> ListarRubrosAsync(CancellationToken ct = default);

    Task<List<OpcionCatalogo>> ListarTecnicasAsync(CancellationToken ct = default);
}
