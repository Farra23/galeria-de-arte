namespace Galeria.Application.Common;

/// <summary>
/// Permite que un caso de uso que escribe en varias tablas quede todo-o-nada.
/// <para>
/// Hace falta porque varios servicios guardan en dos tandas: primero la entidad principal (para
/// obtener su Id autonumérico) y después lo que depende de ese Id — stock, movimientos, marcas.
/// Sin transacción, si la segunda tanda falla (disco lleno, "database is locked", el servicio
/// se reinicia justo ahí) la primera ya quedó escrita: una venta registrada sin descontar el
/// stock, o una liquidación confirmada con los adelantos sin marcar como descontados, que
/// entonces se vuelven a descontar en la liquidación siguiente. Eso es plata mal calculada, y
/// la liquidación es irreversible por diseño.
/// </para>
/// <para>
/// La interfaz vive en Application y la implementación en Infrastructure (mismo criterio que los
/// repositorios): el caso de uso pide "esto es atómico" sin saber que abajo hay EF Core.
/// </para>
/// </summary>
public interface IUnitOfWork
{
    Task<T> EjecutarEnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken ct = default);
}
