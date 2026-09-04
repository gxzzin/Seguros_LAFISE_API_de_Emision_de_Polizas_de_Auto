using Lafise.Insurance.Application.Abstractions;
using Lafise.Insurance.Domain.Entities;
using Lafise.Insurance.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lafise.Insurance.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="IPolicyRepository"/>
public class PolicyRepository : IPolicyRepository
{
    private readonly InsuranceDbContext _context;

    public PolicyRepository(InsuranceDbContext context) => _context = context;

    public async Task<IReadOnlyList<Policy>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Policies
            .AsNoTracking()
            .Include(policy => policy.Customer)
            .Include(policy => policy.Vehicle)
            .OrderByDescending(policy => policy.IssueDate)
            .ThenByDescending(policy => policy.Id)
            .ToListAsync(cancellationToken);

    public Task<Policy?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Policies
            .AsNoTracking()
            .Include(policy => policy.Customer)
            .Include(policy => policy.Vehicle)
            .Include(policy => policy.Coverages)
                .ThenInclude(policyCoverage => policyCoverage.Coverage)
            .FirstOrDefaultAsync(policy => policy.Id == id, cancellationToken);

    public Task<bool> HasActivePolicyForPlateAsync(
        int customerId,
        string normalizedPlate,
        CancellationToken cancellationToken = default) =>
        _context.Policies.AnyAsync(
            policy => policy.CustomerId == customerId
                      && policy.Status == PolicyStatus.Active
                      && policy.Vehicle.Plate == normalizedPlate,
            cancellationToken);

    public Task<bool> HasActivePoliciesForCustomerAsync(
        int customerId,
        CancellationToken cancellationToken = default) =>
        _context.Policies.AnyAsync(
            policy => policy.CustomerId == customerId && policy.Status == PolicyStatus.Active,
            cancellationToken);

    public Task<bool> HasActivePoliciesForVehicleAsync(
        int vehicleId,
        CancellationToken cancellationToken = default) =>
        _context.Policies.AnyAsync(
            policy => policy.VehicleId == vehicleId && policy.Status == PolicyStatus.Active,
            cancellationToken);

    public async Task AddAsync(Policy policy, CancellationToken cancellationToken = default) =>
        await _context.Policies.AddAsync(policy, cancellationToken);
}
