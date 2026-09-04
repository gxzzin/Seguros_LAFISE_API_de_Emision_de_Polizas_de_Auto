using Lafise.Insurance.Application.Abstractions;
using Lafise.Insurance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lafise.Insurance.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="ICustomerRepository"/>
public class CustomerRepository : ICustomerRepository
{
    private readonly InsuranceDbContext _context;

    public CustomerRepository(InsuranceDbContext context) => _context = context;

    public async Task<IReadOnlyList<Customer>> GetAllAsync(
        bool onlyActive,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Customers.AsNoTracking();

        if (onlyActive)
        {
            query = query.Where(customer => customer.IsActive);
        }

        return await query
            .OrderBy(customer => customer.Name)
            .ToListAsync(cancellationToken);
    }

    // Sin AsNoTracking: la entidad se devuelve rastreada para poder actualizarla.
    public Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Customers.FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);

    public Task<bool> ExistsByIdentificationAsync(
        string identificationNumber,
        int? excludeCustomerId = null,
        CancellationToken cancellationToken = default) =>
        _context.Customers.AnyAsync(
            customer => customer.IdentificationNumber == identificationNumber
                        && (excludeCustomerId == null || customer.Id != excludeCustomerId),
            cancellationToken);

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default) =>
        await _context.Customers.AddAsync(customer, cancellationToken);
}
