using Lafise.Insurance.Domain.Entities;

namespace Lafise.Insurance.Application.Abstractions;

public interface IPolicyRepository
{
    Task<IReadOnlyList<Policy>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Policy?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si el cliente ya tiene una póliza activa para la placa indicada.
    /// </summary>
    Task<bool> HasActivePolicyForPlateAsync(
        int customerId,
        string normalizedPlate,
        CancellationToken cancellationToken = default);

    /// <summary>Indica si el cliente tiene pólizas activas; bloquea su desactivación.</summary>
    Task<bool> HasActivePoliciesForCustomerAsync(int customerId, CancellationToken cancellationToken = default);

    /// <summary>Indica si el vehículo tiene pólizas activas; bloquea su desactivación.</summary>
    Task<bool> HasActivePoliciesForVehicleAsync(int vehicleId, CancellationToken cancellationToken = default);

    Task AddAsync(Policy policy, CancellationToken cancellationToken = default);
}
