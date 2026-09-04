using Lafise.Insurance.Application.Contracts.Policies;
using Lafise.Insurance.Application.Options;
using Lafise.Insurance.Application.Services;
using Lafise.Insurance.Domain.Entities;
using Lafise.Insurance.Domain.Enums;
using Lafise.Insurance.Domain.Exceptions;
using Lafise.Insurance.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lafise.Insurance.UnitTests;

/// <summary>
/// Pruebas de la lógica de emisión: cálculo de la prima y reglas de validación.
/// </summary>
public class PolicyServiceTests
{
    private static readonly DateTime IssueDate = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryStore _store = new();
    private readonly PolicyService _service;

    public PolicyServiceTests()
    {
        _store.Customers.Add(new Customer
        {
            Id = 1,
            Name = "Maria Fernanda Lopez",
            IdentificationNumber = "001-120589-1002B",
            Email = "maria.lopez@example.com"
        });

        _store.Coverages.AddRange(new[]
        {
            new Coverage { Id = 1, Name = "Robo", Rate = 2.50m, IsActive = true },
            new Coverage { Id = 2, Name = "Choque", Rate = 3.25m, IsActive = true },
            new Coverage { Id = 3, Name = "Responsabilidad Civil", Rate = 1.75m, IsActive = true },
            new Coverage { Id = 6, Name = "Asistencia Vial", Rate = 0.40m, IsActive = false }
        });

        var options = Options.Create(new UnderwritingOptions());

        _service = new PolicyService(
            new FakePolicyRepository(_store),
            new FakeCustomerRepository(_store),
            new FakeVehicleRepository(_store),
            new FakeCoverageRepository(_store),
            new InMemoryPolicyNumberGenerator(),
            new PlateValidator(options),
            new FakeUnitOfWork(_store),
            new FixedDateTimeProvider(IssueDate),
            options,
            NullLogger<PolicyService>.Instance);
    }

    [Fact]
    public async Task IssueAsync_WithValidRequest_CalculatesPremiumFromCoverageRates()
    {
        // Robo 2.50% + Choque 3.25% = 5.75% sobre 20,000.00 = 1,150.00
        var request = BuildRequest(coverageIds: new List<int> { 1, 2 }, commercialValue: 20_000m);

        var policy = await _service.IssueAsync(request);

        Assert.Equal(20_000m, policy.InsuredAmount);
        Assert.Equal(1_150m, policy.TotalPremium);
        Assert.Equal(2, policy.Coverages.Count);
        Assert.Equal(500m, policy.Coverages.Single(c => c.CoverageId == 1).PremiumAmount);
        Assert.Equal(650m, policy.Coverages.Single(c => c.CoverageId == 2).PremiumAmount);
        Assert.Equal(1, _store.SaveChangesCallCount);
    }

    [Fact]
    public async Task IssueAsync_WithValidRequest_GeneratesPolicyNumberAndActiveStatus()
    {
        var policy = await _service.IssueAsync(BuildRequest());

        Assert.Equal("AU-0000001-000001-0", policy.PolicyNumber);
        Assert.Equal(nameof(PolicyStatus.Active), policy.Status);
        Assert.Equal(IssueDate, policy.IssueDate);
        Assert.Equal(IssueDate.AddMonths(12), policy.ExpirationDate);
    }

    [Fact]
    public async Task IssueAsync_StoresTheRateAppliedAtIssuance()
    {
        var policy = await _service.IssueAsync(BuildRequest(coverageIds: new List<int> { 1 }));

        // Un cambio posterior en el catálogo no debe alterar la póliza ya emitida.
        _store.Coverages.Single(coverage => coverage.Id == 1).Rate = 9.99m;

        Assert.Equal(2.50m, policy.Coverages.Single().AppliedRate);
    }

    [Theory]
    [InlineData(2005)] // 21 años de antigüedad
    [InlineData(1998)]
    public async Task IssueAsync_WhenVehicleExceedsMaximumAge_ThrowsBusinessRuleViolation(int year)
    {
        var request = BuildRequest(year: year);

        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(() => _service.IssueAsync(request));

        Assert.Equal("VEHICLE_TOO_OLD", exception.Code);
    }

    [Fact]
    public async Task IssueAsync_WhenVehicleIsExactlyAtTheAgeLimit_Succeeds()
    {
        // 2026 - 2006 = 20 años: es el límite permitido, no debe rechazarse.
        var policy = await _service.IssueAsync(BuildRequest(year: 2006));

        Assert.Equal(2006, policy.Vehicle.Year);
    }

    [Fact]
    public async Task IssueAsync_WhenVehicleYearIsInTheFuture_ThrowsBusinessRuleViolation()
    {
        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _service.IssueAsync(BuildRequest(year: 2030)));

        Assert.Equal("INVALID_VEHICLE_YEAR", exception.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IssueAsync_WithoutPlate_ThrowsBusinessRuleViolation(string plate)
    {
        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _service.IssueAsync(BuildRequest(plate: plate)));

        Assert.Equal("PLATE_REQUIRED", exception.Code);
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("ABCD1234")]
    [InlineData("AB")]
    [InlineData("M12")]
    public async Task IssueAsync_WithInvalidPlateFormat_ThrowsBusinessRuleViolation(string plate)
    {
        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _service.IssueAsync(BuildRequest(plate: plate)));

        Assert.Equal("INVALID_PLATE_FORMAT", exception.Code);
    }

    [Theory]
    [InlineData("M 105432", "M105432")] // Formato nicaragüense: letra departamental + 6 dígitos.
    [InlineData("m 123456", "M123456")]
    [InlineData("abc-1234", "ABC1234")]
    [InlineData(" MG-987 ", "MG987")]
    public async Task IssueAsync_NormalizesThePlateBeforeStoringIt(string input, string expected)
    {
        var policy = await _service.IssueAsync(BuildRequest(plate: input));

        Assert.Equal(expected, policy.Vehicle.Plate);
    }

    [Fact]
    public async Task IssueAsync_WhenCustomerAlreadyHasAnActivePolicyForThePlate_ThrowsConflict()
    {
        await _service.IssueAsync(BuildRequest(plate: "M123456"));

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _service.IssueAsync(BuildRequest(plate: "m-123456")));

        Assert.Equal("DUPLICATE_ACTIVE_POLICY", exception.Code);
    }

    [Fact]
    public async Task IssueAsync_WhenThePreviousPolicyIsCancelled_AllowsANewIssuance()
    {
        var first = await _service.IssueAsync(BuildRequest(plate: "M123456"));
        _store.Policies.Single(policy => policy.Id == first.Id).Status = PolicyStatus.Cancelled;

        var second = await _service.IssueAsync(BuildRequest(plate: "M123456"));

        Assert.Equal("AU-0000002-000002-0", second.PolicyNumber);
    }

    [Fact]
    public async Task IssueAsync_ReusesTheVehicleWhenThePlateAlreadyExists()
    {
        await _service.IssueAsync(BuildRequest(plate: "M123456"));
        _store.Policies.Single().Status = PolicyStatus.Expired;

        await _service.IssueAsync(BuildRequest(plate: "M123456", commercialValue: 18_000m));

        Assert.Single(_store.Vehicles);
        Assert.Equal(18_000m, _store.Vehicles.Single().CommercialValue);
    }

    [Fact]
    public async Task IssueAsync_WhenCustomerIsInactive_ThrowsBusinessRuleViolation()
    {
        _store.Customers.Single().IsActive = false;

        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _service.IssueAsync(BuildRequest()));

        Assert.Equal("INACTIVE_CUSTOMER", exception.Code);
    }

    [Fact]
    public async Task IssueAsync_ReactivatesAVehicleThatWasDeactivated()
    {
        await _service.IssueAsync(BuildRequest(plate: "M123456"));
        _store.Policies.Single().Status = PolicyStatus.Cancelled;
        _store.Vehicles.Single().IsActive = false;

        var policy = await _service.IssueAsync(BuildRequest(plate: "M123456"));

        Assert.True(_store.Vehicles.Single().IsActive);
        Assert.Equal("M123456", policy.Vehicle.Plate);
    }

    [Fact]
    public async Task IssueAsync_WhenCustomerDoesNotExist_ThrowsEntityNotFound()
    {
        var request = BuildRequest();
        request.CustomerId = 999;

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.IssueAsync(request));
    }

    [Fact]
    public async Task IssueAsync_WhenACoverageDoesNotExist_ThrowsEntityNotFound()
    {
        var request = BuildRequest(coverageIds: new List<int> { 1, 99 });

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.IssueAsync(request));
    }

    [Fact]
    public async Task IssueAsync_WithAnInactiveCoverage_ThrowsBusinessRuleViolation()
    {
        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _service.IssueAsync(BuildRequest(coverageIds: new List<int> { 1, 6 })));

        Assert.Equal("INACTIVE_COVERAGE", exception.Code);
    }

    [Fact]
    public async Task IssueAsync_WithRepeatedCoverages_ThrowsBusinessRuleViolation()
    {
        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _service.IssueAsync(BuildRequest(coverageIds: new List<int> { 1, 1 })));

        Assert.Equal("DUPLICATED_COVERAGES", exception.Code);
    }

    [Fact]
    public async Task IssueAsync_WithoutCoverages_ThrowsBusinessRuleViolation()
    {
        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _service.IssueAsync(BuildRequest(coverageIds: new List<int>())));

        Assert.Equal("COVERAGES_REQUIRED", exception.Code);
    }

    [Fact]
    public async Task IssueAsync_RoundsEachCoveragePremiumToTwoDecimals()
    {
        // 1.75% de 12,345.67 = 216.049225 -> 216.05
        var request = BuildRequest(coverageIds: new List<int> { 3 }, commercialValue: 12_345.67m);

        var policy = await _service.IssueAsync(request);

        Assert.Equal(216.05m, policy.TotalPremium);
    }

    [Fact]
    public async Task GetByIdAsync_WhenThePolicyDoesNotExist_ThrowsEntityNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.GetByIdAsync(404));
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsTheIssuedPolicies()
    {
        await _service.IssueAsync(BuildRequest(plate: "M123456"));
        await _service.IssueAsync(BuildRequest(plate: "ABC1234"));

        var history = await _service.GetHistoryAsync();

        Assert.Equal(2, history.Count);
        Assert.All(history, summary => Assert.Equal(nameof(PolicyStatus.Active), summary.Status));
    }

    private static IssuePolicyRequest BuildRequest(
        int customerId = 1,
        string plate = "M123456",
        int year = 2022,
        decimal commercialValue = 20_000m,
        List<int>? coverageIds = null) =>
        new()
        {
            CustomerId = customerId,
            Vehicle = new VehicleRequest
            {
                Plate = plate,
                Brand = "Toyota",
                Model = "Corolla",
                Year = year,
                CommercialValue = commercialValue
            },
            CoverageIds = coverageIds ?? new List<int> { 1, 2 }
        };
}
