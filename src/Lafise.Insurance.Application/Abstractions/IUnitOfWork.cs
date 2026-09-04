namespace Lafise.Insurance.Application.Abstractions;

/// <summary>
/// Confirma en una sola transacción los cambios acumulados por los repositorios.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
