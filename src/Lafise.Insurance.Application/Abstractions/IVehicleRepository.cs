using Lafise.Insurance.Domain.Entities;

namespace Lafise.Insurance.Application.Abstractions;

public interface IVehicleRepository
{
    Task<IReadOnlyList<Vehicle>> GetAllAsync(bool onlyActive, CancellationToken cancellationToken = default);

    Task<Vehicle?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Vehicle?> GetByPlateAsync(string normalizedPlate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si la placa ya está registrada. <paramref name="excludeVehicleId"/> permite omitir
    /// al propio vehículo durante una actualización.
    /// </summary>
    Task<bool> ExistsByPlateAsync(
        string normalizedPlate,
        int? excludeVehicleId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
}
