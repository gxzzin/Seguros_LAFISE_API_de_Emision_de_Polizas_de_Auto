using Lafise.Insurance.Application.Abstractions;
using Lafise.Insurance.Domain.Entities;
using Lafise.Insurance.Domain.Enums;

namespace Lafise.Insurance.UnitTests.TestDoubles;

/// <summary>
/// Almacén en memoria compartido por los repositorios falsos. Permite probar la lógica
/// de negocio sin depender de EF Core ni de una base de datos real.
/// </summary>
public class InMemoryStore
{
    public List<Customer> Customers { get; } = new();

    public List<Vehicle> Vehicles { get; } = new();

    public List<Coverage> Coverages { get; } = new();

    public List<Policy> Policies { get; } = new();

    public int SaveChangesCallCount { get; set; }
}

public class FakeCustomerRepository : ICustomerRepository
{
    private readonly InMemoryStore _store;

    public FakeCustomerRepository(InMemoryStore store) => _store = store;

    public Task<IReadOnlyList<Customer>> GetAllAsync(bool onlyActive, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Customer>>(_store.Customers
            .Where(customer => !onlyActive || customer.IsActive)
            .OrderBy(customer => customer.Name)
            .ToList());

    public Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Customers.FirstOrDefault(customer => customer.Id == id));

    public Task<bool> ExistsByIdentificationAsync(
        string identificationNumber,
        int? excludeCustomerId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Customers.Any(customer =>
            customer.IdentificationNumber == identificationNumber
            && (excludeCustomerId == null || customer.Id != excludeCustomerId)));

    public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        customer.Id = _store.Customers.Count + 1;
        _store.Customers.Add(customer);
        return Task.CompletedTask;
    }
}

public class FakeVehicleRepository : IVehicleRepository
{
    private readonly InMemoryStore _store;

    public FakeVehicleRepository(InMemoryStore store) => _store = store;

    public Task<IReadOnlyList<Vehicle>> GetAllAsync(bool onlyActive, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Vehicle>>(_store.Vehicles
            .Where(vehicle => !onlyActive || vehicle.IsActive)
            .OrderBy(vehicle => vehicle.Plate)
            .ToList());

    public Task<Vehicle?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Vehicles.FirstOrDefault(vehicle => vehicle.Id == id));

    public Task<Vehicle?> GetByPlateAsync(string normalizedPlate, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Vehicles.FirstOrDefault(vehicle => vehicle.Plate == normalizedPlate));

    public Task<bool> ExistsByPlateAsync(
        string normalizedPlate,
        int? excludeVehicleId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Vehicles.Any(vehicle =>
            vehicle.Plate == normalizedPlate
            && (excludeVehicleId == null || vehicle.Id != excludeVehicleId)));

    public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        vehicle.Id = _store.Vehicles.Count + 1;
        _store.Vehicles.Add(vehicle);
        return Task.CompletedTask;
    }
}

public class FakeCoverageRepository : ICoverageRepository
{
    private readonly InMemoryStore _store;

    public FakeCoverageRepository(InMemoryStore store) => _store = store;

    public Task<IReadOnlyList<Coverage>> GetAllAsync(bool onlyActive, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Coverage>>(
            _store.Coverages.Where(coverage => !onlyActive || coverage.IsActive).ToList());

    public Task<Coverage?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Coverages.FirstOrDefault(coverage => coverage.Id == id));

    public Task<IReadOnlyList<Coverage>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToHashSet();
        return Task.FromResult<IReadOnlyList<Coverage>>(
            _store.Coverages.Where(coverage => idList.Contains(coverage.Id)).ToList());
    }

    public Task<bool> ExistsByNameAsync(
        string name,
        int? excludeCoverageId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Coverages.Any(coverage =>
            coverage.Name == name && (excludeCoverageId == null || coverage.Id != excludeCoverageId)));

    public Task AddAsync(Coverage coverage, CancellationToken cancellationToken = default)
    {
        coverage.Id = _store.Coverages.Count == 0 ? 1 : _store.Coverages.Max(c => c.Id) + 1;
        _store.Coverages.Add(coverage);
        return Task.CompletedTask;
    }
}

public class FakePolicyRepository : IPolicyRepository
{
    private readonly InMemoryStore _store;

    public FakePolicyRepository(InMemoryStore store) => _store = store;

    public Task<IReadOnlyList<Policy>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Policy>>(
            _store.Policies.OrderByDescending(policy => policy.IssueDate).ToList());

    public Task<Policy?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Policies.FirstOrDefault(policy => policy.Id == id));

    public Task<bool> HasActivePolicyForPlateAsync(int customerId, string normalizedPlate, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Policies.Any(policy =>
            policy.CustomerId == customerId
            && policy.Status == PolicyStatus.Active
            && policy.Vehicle.Plate == normalizedPlate));

    public Task<bool> HasActivePoliciesForCustomerAsync(int customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Policies.Any(policy =>
            policy.CustomerId == customerId && policy.Status == PolicyStatus.Active));

    public Task<bool> HasActivePoliciesForVehicleAsync(int vehicleId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Policies.Any(policy =>
            policy.VehicleId == vehicleId && policy.Status == PolicyStatus.Active));

    public Task AddAsync(Policy policy, CancellationToken cancellationToken = default)
    {
        policy.Id = _store.Policies.Count + 1;
        _store.Policies.Add(policy);
        return Task.CompletedTask;
    }
}

public class FakeUnitOfWork : IUnitOfWork
{
    private readonly InMemoryStore _store;

    public FakeUnitOfWork(InMemoryStore store) => _store = store;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _store.SaveChangesCallCount++;
        return Task.FromResult(1);
    }
}
