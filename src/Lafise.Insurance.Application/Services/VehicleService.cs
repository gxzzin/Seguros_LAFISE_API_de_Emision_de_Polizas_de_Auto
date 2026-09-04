using Lafise.Insurance.Application.Abstractions;
using Lafise.Insurance.Application.Contracts.Vehicles;
using Lafise.Insurance.Application.Mapping;
using Lafise.Insurance.Domain.Entities;
using Lafise.Insurance.Domain.Exceptions;

namespace Lafise.Insurance.Application.Services;

/// <summary>CRUD del catálogo de vehículos.</summary>
public interface IVehicleService
{
    Task<IReadOnlyList<VehicleDto>> GetAllAsync(bool onlyActive, CancellationToken cancellationToken = default);

    Task<VehicleDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<VehicleDto> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default);

    Task<VehicleDto> UpdateAsync(int id, UpdateVehicleRequest request, CancellationToken cancellationToken = default);

    /// <summary>Baja lógica del vehículo. No elimina la fila para conservar el historial.</summary>
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IVehicleService"/>
public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IPolicyRepository _policyRepository;
    private readonly IPlateValidator _plateValidator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public VehicleService(
        IVehicleRepository vehicleRepository,
        IPolicyRepository policyRepository,
        IPlateValidator plateValidator,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _vehicleRepository = vehicleRepository;
        _policyRepository = policyRepository;
        _plateValidator = plateValidator;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<VehicleDto>> GetAllAsync(
        bool onlyActive,
        CancellationToken cancellationToken = default)
    {
        var vehicles = await _vehicleRepository.GetAllAsync(onlyActive, cancellationToken);
        return vehicles.Select(vehicle => vehicle.ToDto()).ToList();
    }

    public async Task<VehicleDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var vehicle = await FindOrThrowAsync(id, cancellationToken);
        return vehicle.ToDto();
    }

    public async Task<VehicleDto> CreateAsync(
        CreateVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        // Se valida el formato de la placa con la misma regla que usa la emisión.
        var plate = _plateValidator.NormalizeAndValidate(request.Plate);
        await EnsurePlateIsAvailableAsync(plate, null, cancellationToken);

        var vehicle = new Vehicle
        {
            Plate = plate,
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            Year = request.Year,
            CommercialValue = request.CommercialValue,
            IsActive = request.IsActive,
            CreatedAtUtc = _dateTimeProvider.UtcNow
        };

        await _vehicleRepository.AddAsync(vehicle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return vehicle.ToDto();
    }

    public async Task<VehicleDto> UpdateAsync(
        int id,
        UpdateVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        var vehicle = await FindOrThrowAsync(id, cancellationToken);

        var plate = _plateValidator.NormalizeAndValidate(request.Plate);
        await EnsurePlateIsAvailableAsync(plate, id, cancellationToken);

        // Desactivar por PUT queda sujeto a la misma regla que el DELETE.
        if (vehicle.IsActive && !request.IsActive)
        {
            await EnsureHasNoActivePoliciesAsync(vehicle, cancellationToken);
        }

        vehicle.Plate = plate;
        vehicle.Brand = request.Brand.Trim();
        vehicle.Model = request.Model.Trim();
        vehicle.Year = request.Year;
        vehicle.CommercialValue = request.CommercialValue;
        vehicle.IsActive = request.IsActive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return vehicle.ToDto();
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var vehicle = await FindOrThrowAsync(id, cancellationToken);

        if (!vehicle.IsActive)
        {
            return;
        }

        await EnsureHasNoActivePoliciesAsync(vehicle, cancellationToken);

        vehicle.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Vehicle> FindOrThrowAsync(int id, CancellationToken cancellationToken) =>
        await _vehicleRepository.GetByIdAsync(id, cancellationToken)
        ?? throw new EntityNotFoundException("el vehículo", id);

    private async Task EnsurePlateIsAvailableAsync(
        string normalizedPlate,
        int? excludeVehicleId,
        CancellationToken cancellationToken)
    {
        if (await _vehicleRepository.ExistsByPlateAsync(normalizedPlate, excludeVehicleId, cancellationToken))
        {
            throw new ConflictException(
                "VEHICLE_ALREADY_EXISTS",
                $"Ya existe un vehículo registrado con la placa '{normalizedPlate}'.");
        }
    }

    private async Task EnsureHasNoActivePoliciesAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        if (await _policyRepository.HasActivePoliciesForVehicleAsync(vehicle.Id, cancellationToken))
        {
            throw new ConflictException(
                "VEHICLE_HAS_ACTIVE_POLICIES",
                $"No se puede dar de baja el vehículo con placa '{vehicle.Plate}' porque tiene pólizas activas.");
        }
    }
}
