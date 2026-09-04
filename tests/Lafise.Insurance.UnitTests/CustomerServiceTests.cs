using Lafise.Insurance.Application.Contracts.Customers;
using Lafise.Insurance.Application.Options;
using Lafise.Insurance.Application.Services;
using Lafise.Insurance.Domain.Entities;
using Lafise.Insurance.Domain.Enums;
using Lafise.Insurance.Domain.Exceptions;
using Lafise.Insurance.UnitTests.TestDoubles;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lafise.Insurance.UnitTests;

/// <summary>Pruebas del CRUD de clientes, con foco en las reglas de la baja lógica.</summary>
public class CustomerServiceTests
{
    private readonly InMemoryStore _store = new();
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _store.Customers.AddRange(new[]
        {
            new Customer
            {
                Id = 1,
                Name = "Maria Fernanda Lopez",
                IdentificationNumber = "001-120589-1002B",
                Email = "maria.lopez@example.com",
                IsActive = true
            },
            new Customer
            {
                Id = 2,
                Name = "Carlos Alberto Mendoza",
                IdentificationNumber = "281-030777-0005X",
                Email = "carlos.mendoza@example.com",
                IsActive = true
            },
            new Customer
            {
                Id = 3,
                Name = "Cliente Dado De Baja",
                IdentificationNumber = "555-150380-0099C",
                Email = "baja@example.com",
                IsActive = false
            }
        });

        _service = new CustomerService(
            new FakeCustomerRepository(_store),
            new FakePolicyRepository(_store),
            new IdentificationValidator(Options.Create(new IdentificationOptions())),
            new FakeUnitOfWork(_store),
            new FixedDateTimeProvider(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public async Task GetAllAsync_WhenOnlyActive_OmitsDeactivatedCustomers()
    {
        var customers = await _service.GetAllAsync(onlyActive: true);

        Assert.Equal(2, customers.Count);
        Assert.DoesNotContain(customers, customer => customer.Id == 3);
    }

    [Fact]
    public async Task GetAllAsync_WhenNotOnlyActive_IncludesDeactivatedCustomers()
    {
        var customers = await _service.GetAllAsync(onlyActive: false);

        Assert.Equal(3, customers.Count);
    }

    [Fact]
    public async Task UpdateAsync_ChangesTheEditableFields()
    {
        var updated = await _service.UpdateAsync(1, new UpdateCustomerRequest
        {
            Name = "Maria F. Lopez Reyes",
            IdentificationNumber = "001-120589-1002B",
            Email = "mflopez@example.com",
            IsActive = true
        });

        Assert.Equal("Maria F. Lopez Reyes", updated.Name);
        Assert.Equal("mflopez@example.com", updated.Email);
        Assert.Equal(1, _store.SaveChangesCallCount);
    }

    [Fact]
    public async Task UpdateAsync_KeepingItsOwnIdentification_DoesNotReportConflict()
    {
        var updated = await _service.UpdateAsync(1, BuildUpdateRequest(identification: "001-120589-1002B"));

        Assert.Equal("001-120589-1002B", updated.IdentificationNumber);
    }

    [Fact]
    public async Task UpdateAsync_WithAnIdentificationOfAnotherCustomer_ThrowsConflict()
    {
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _service.UpdateAsync(1, BuildUpdateRequest(identification: "281-030777-0005X")));

        Assert.Equal("CUSTOMER_ALREADY_EXISTS", exception.Code);
    }

    [Fact]
    public async Task UpdateAsync_CanReactivateADeactivatedCustomer()
    {
        var updated = await _service.UpdateAsync(3, new UpdateCustomerRequest
        {
            Name = "Cliente Reactivado",
            IdentificationNumber = "555-150380-0099C",
            Email = "baja@example.com",
            IsActive = true
        });

        Assert.True(updated.IsActive);
    }

    [Fact]
    public async Task CreateAsync_NormalizesTheIdentificationBeforeCheckingForDuplicates()
    {
        // La misma cédula en minúscula no debe crear un segundo cliente.
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateAsync(new CreateCustomerRequest
            {
                Name = "Impostor Con Minusculas",
                IdentificationNumber = "001-120589-1002b",
                Email = "impostor@example.com"
            }));

        Assert.Equal("CUSTOMER_ALREADY_EXISTS", exception.Code);
    }

    [Fact]
    public async Task CreateAsync_WithAnInvalidIdentification_ThrowsBusinessRuleViolation()
    {
        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _service.CreateAsync(new CreateCustomerRequest
            {
                Name = "Identificacion Invalida",
                IdentificationNumber = "12345",
                Email = "invalida@example.com"
            }));

        Assert.Equal("INVALID_IDENTIFICATION_FORMAT", exception.Code);
    }

    [Fact]
    public async Task UpdateAsync_WhenCustomerDoesNotExist_ThrowsEntityNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.UpdateAsync(404, BuildUpdateRequest()));
    }

    [Fact]
    public async Task DeactivateAsync_MarksTheCustomerAsInactiveWithoutDeletingTheRow()
    {
        await _service.DeactivateAsync(1);

        Assert.Single(_store.Customers, customer => customer.Id == 1);
        Assert.False(_store.Customers.Single(customer => customer.Id == 1).IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_WhenTheCustomerHasActivePolicies_ThrowsConflict()
    {
        AddPolicy(customerId: 1, PolicyStatus.Active);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => _service.DeactivateAsync(1));

        Assert.Equal("CUSTOMER_HAS_ACTIVE_POLICIES", exception.Code);
        Assert.True(_store.Customers.Single(customer => customer.Id == 1).IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_WhenThePoliciesAreNoLongerActive_Succeeds()
    {
        AddPolicy(customerId: 1, PolicyStatus.Expired);

        await _service.DeactivateAsync(1);

        Assert.False(_store.Customers.Single(customer => customer.Id == 1).IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_OnAnAlreadyInactiveCustomer_IsIdempotent()
    {
        await _service.DeactivateAsync(3);

        Assert.Equal(0, _store.SaveChangesCallCount);
    }

    [Fact]
    public async Task DeactivateAsync_WhenCustomerDoesNotExist_ThrowsEntityNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.DeactivateAsync(404));
    }

    private void AddPolicy(int customerId, PolicyStatus status) =>
        _store.Policies.Add(new Policy
        {
            Id = _store.Policies.Count + 1,
            PolicyNumber = $"AU-{_store.Policies.Count + 1:D7}-{_store.Policies.Count + 1:D6}-0",
            CustomerId = customerId,
            VehicleId = 1,
            Status = status
        });

    private static UpdateCustomerRequest BuildUpdateRequest(
        string name = "Maria Fernanda Lopez",
        string identification = "001-120589-1002B",
        string email = "maria.lopez@example.com",
        bool isActive = true) =>
        new()
        {
            Name = name,
            IdentificationNumber = identification,
            Email = email,
            IsActive = isActive
        };
}
