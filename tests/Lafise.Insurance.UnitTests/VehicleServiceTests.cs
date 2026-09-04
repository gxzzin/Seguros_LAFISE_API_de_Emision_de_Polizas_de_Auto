using Lafise.Insurance.Application.Contracts.Vehicles;
using Lafise.Insurance.Application.Options;
using Lafise.Insurance.Application.Services;
using Lafise.Insurance.Domain.Entities;
using Lafise.Insurance.Domain.Enums;
using Lafise.Insurance.Domain.Exceptions;
using Lafise.Insurance.UnitTests.TestDoubles;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lafise.Insurance.UnitTests;

/// <summary>Pruebas del CRUD de vehículos: formato de placa, unicidad y baja lógica.</summary>
public class VehicleServiceTests
{
    private readonly InMemoryStore _store = new();
    private readonly VehicleService _service;

    public VehicleServiceTests()
    {
        _store.Vehicles.AddRange(new[]
        {
            new Vehicle
            {
                Id = 1,
                Plate = "M123456",
                Brand = "Toyota",
                Model = "Corolla",
                Year = 2022,
                CommercialValue = 20_000m,
                IsActive = true
            },
            new Vehicle
            {
                Id = 2,
                Plate = "ABC1234",
                Brand = "Nissan",
                Model = "Sentra",
                Year = 2019,
                CommercialValue = 12_000m,
                IsActive = true
            }
        });

        _service = new VehicleService(
            new FakeVehicleRepository(_store),
            new FakePolicyRepository(_store),
            new PlateValidator(Options.Create(new UnderwritingOptions())),
            new FakeUnitOfWork(_store),
            new FixedDateTimeProvider(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc)));
    }

    [Theory]
    [InlineData("xy-4321", "XY4321")]
    [InlineData(" mg 987 ", "MG987")]
    public async Task CreateAsync_NormalizesThePlate(string input, string expected)
    {
        var vehicle = await _service.CreateAsync(BuildCreateRequest(plate: input));

        Assert.Equal(expected, vehicle.Plate);
        Assert.True(vehicle.IsActive);
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("ABCD1234")]
    [InlineData("")]
    public async Task CreateAsync_WithAnInvalidPlate_ThrowsBusinessRuleViolation(string plate)
    {
        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _service.CreateAsync(BuildCreateRequest(plate: plate)));
    }

    [Fact]
    public async Task CreateAsync_WithAnExistingPlate_ThrowsConflict()
    {
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateAsync(BuildCreateRequest(plate: "m-123456")));

        Assert.Equal("VEHICLE_ALREADY_EXISTS", exception.Code);
    }

    [Fact]
    public async Task CreateAsync_DoesNotApplyTheUnderwritingAgeLimit()
    {
        // Registrar un vehículo antiguo en el catálogo es válido; la antigüedad sólo
        // bloquea la emisión de la póliza.
        var vehicle = await _service.CreateAsync(BuildCreateRequest(plate: "OLD999", year: 1995));

        Assert.Equal(1995, vehicle.Year);
    }

    [Fact]
    public async Task UpdateAsync_ChangesTheEditableFields()
    {
        var updated = await _service.UpdateAsync(1, BuildUpdateRequest(commercialValue: 17_500m));

        Assert.Equal(17_500m, updated.CommercialValue);
        Assert.Equal(1, _store.SaveChangesCallCount);
    }

    [Fact]
    public async Task UpdateAsync_KeepingItsOwnPlate_DoesNotReportConflict()
    {
        var updated = await _service.UpdateAsync(1, BuildUpdateRequest(plate: "M123456"));

        Assert.Equal("M123456", updated.Plate);
    }

    [Fact]
    public async Task UpdateAsync_WithThePlateOfAnotherVehicle_ThrowsConflict()
    {
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _service.UpdateAsync(1, BuildUpdateRequest(plate: "ABC1234")));

        Assert.Equal("VEHICLE_ALREADY_EXISTS", exception.Code);
    }

    [Fact]
    public async Task UpdateAsync_WhenVehicleDoesNotExist_ThrowsEntityNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.UpdateAsync(404, BuildUpdateRequest()));
    }

    [Fact]
    public async Task DeactivateAsync_MarksTheVehicleAsInactiveWithoutDeletingTheRow()
    {
        await _service.DeactivateAsync(1);

        Assert.Equal(2, _store.Vehicles.Count);
        Assert.False(_store.Vehicles.Single(vehicle => vehicle.Id == 1).IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_WhenTheVehicleHasActivePolicies_ThrowsConflict()
    {
        _store.Policies.Add(new Policy
        {
            Id = 1,
            PolicyNumber = "AU-0000001-000001-0",
            CustomerId = 1,
            VehicleId = 1,
            Status = PolicyStatus.Active
        });

        var exception = await Assert.ThrowsAsync<ConflictException>(() => _service.DeactivateAsync(1));

        Assert.Equal("VEHICLE_HAS_ACTIVE_POLICIES", exception.Code);
    }

    [Fact]
    public async Task DeactivateAsync_OnAnAlreadyInactiveVehicle_IsIdempotent()
    {
        _store.Vehicles.Single(vehicle => vehicle.Id == 1).IsActive = false;

        await _service.DeactivateAsync(1);

        Assert.Equal(0, _store.SaveChangesCallCount);
    }

    private static CreateVehicleRequest BuildCreateRequest(
        string plate = "XY4321",
        int year = 2021,
        decimal commercialValue = 9_000m) =>
        new()
        {
            Plate = plate,
            Brand = "Kia",
            Model = "Rio",
            Year = year,
            CommercialValue = commercialValue,
            IsActive = true
        };

    private static UpdateVehicleRequest BuildUpdateRequest(
        string plate = "M123456",
        int year = 2022,
        decimal commercialValue = 20_000m,
        bool isActive = true) =>
        new()
        {
            Plate = plate,
            Brand = "Toyota",
            Model = "Corolla",
            Year = year,
            CommercialValue = commercialValue,
            IsActive = isActive
        };
}
