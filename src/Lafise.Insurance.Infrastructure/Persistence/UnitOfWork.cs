using Lafise.Insurance.Application.Abstractions;

namespace Lafise.Insurance.Infrastructure.Persistence;

/// <summary>
/// Implementación de la unidad de trabajo sobre el <see cref="InsuranceDbContext"/>.
/// Un único SaveChanges confirma la póliza y sus coberturas de forma atómica.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly InsuranceDbContext _context;

    public UnitOfWork(InsuranceDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
