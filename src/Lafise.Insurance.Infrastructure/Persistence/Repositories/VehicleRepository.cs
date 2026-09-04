using Lafise.Insurance.Application.Abstractions;
using Lafise.Insurance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lafise.Insurance.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="IVehicleRepository"/>
public class VehicleRepository : IVehicleRepository
{
    private readonly InsuranceDbContext _context;

    public VehicleRepository(InsuranceDbContext context) => _context = context;

    public async Task<IReadOnlyList<Vehicle>> GetAllAsync(
        bool onlyActive,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Vehicles.AsNoTracking();

        if (onlyActive)
        {
            query = query.Where(vehicle => vehicle.IsActive);
        }

        return await query
            .OrderBy(vehicle => vehicle.Plate)
            .ToListAsync(cancellationToken);
    }

    public Task<Vehicle?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Vehicles.FirstOrDefaultAsync(vehicle => vehicle.Id == id, cancellationToken);

    public Task<Vehicle?> GetByPlateAsync(string normalizedPlate, CancellationToken cancellationToken = default) =>
        _context.Vehicles.FirstOrDefaultAsync(vehicle => vehicle.Plate == normalizedPlate, cancellationToken);

    public Task<bool> ExistsByPlateAsync(
        string normalizedPlate,
        int? excludeVehicleId = null,
        CancellationToken cancellationToken = default) =>
        _context.Vehicles.AnyAsync(
            vehicle => vehicle.Plate == normalizedPlate
                       && (excludeVehicleId == null || vehicle.Id != excludeVehicleId),
            cancellationToken);

    public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default) =>
        await _context.Vehicles.AddAsync(vehicle, cancellationToken);
}
