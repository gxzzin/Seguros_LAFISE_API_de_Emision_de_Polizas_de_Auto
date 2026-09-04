using Lafise.Insurance.Application.Abstractions;
using Lafise.Insurance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lafise.Insurance.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="ICoverageRepository"/>
public class CoverageRepository : ICoverageRepository
{
    private readonly InsuranceDbContext _context;

    public CoverageRepository(InsuranceDbContext context) => _context = context;

    public async Task<IReadOnlyList<Coverage>> GetAllAsync(
        bool onlyActive,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Coverages.AsNoTracking();

        if (onlyActive)
        {
            query = query.Where(coverage => coverage.IsActive);
        }

        return await query
            .OrderBy(coverage => coverage.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Coverage?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Coverages.FirstOrDefaultAsync(coverage => coverage.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Coverage>> GetByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();

        return await _context.Coverages
            .Where(coverage => idList.Contains(coverage.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name,
        int? excludeCoverageId = null,
        CancellationToken cancellationToken = default) =>
        _context.Coverages.AnyAsync(
            coverage => coverage.Name == name
                        && (excludeCoverageId == null || coverage.Id != excludeCoverageId),
            cancellationToken);

    public async Task AddAsync(Coverage coverage, CancellationToken cancellationToken = default) =>
        await _context.Coverages.AddAsync(coverage, cancellationToken);
}
