using Lafise.Insurance.Domain.Entities;

namespace Lafise.Insurance.Application.Abstractions;

public interface ICoverageRepository
{
    Task<IReadOnlyList<Coverage>> GetAllAsync(bool onlyActive, CancellationToken cancellationToken = default);

    Task<Coverage?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Coverage>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si el nombre de cobertura ya existe. <paramref name="excludeCoverageId"/> permite
    /// omitir a la propia cobertura durante una actualización.
    /// </summary>
    Task<bool> ExistsByNameAsync(
        string name,
        int? excludeCoverageId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(Coverage coverage, CancellationToken cancellationToken = default);
}
