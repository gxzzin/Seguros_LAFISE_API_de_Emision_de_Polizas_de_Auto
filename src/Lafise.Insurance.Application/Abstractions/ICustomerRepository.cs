using Lafise.Insurance.Domain.Entities;

namespace Lafise.Insurance.Application.Abstractions;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync(bool onlyActive, CancellationToken cancellationToken = default);

    Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si la identificación ya está en uso. <paramref name="excludeCustomerId"/> permite
    /// omitir al propio cliente durante una actualización.
    /// </summary>
    Task<bool> ExistsByIdentificationAsync(
        string identificationNumber,
        int? excludeCustomerId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
}
