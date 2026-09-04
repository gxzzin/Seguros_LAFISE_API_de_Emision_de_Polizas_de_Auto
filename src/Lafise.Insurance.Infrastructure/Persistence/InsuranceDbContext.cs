using Lafise.Insurance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lafise.Insurance.Infrastructure.Persistence;

/// <summary>
/// Contexto de EF Core (enfoque Code First) para el módulo de emisión de pólizas.
/// </summary>
public class InsuranceDbContext : DbContext
{
    /// <summary>Nombre de la secuencia que alimenta el correlativo de la póliza.</summary>
    public const string PolicyNumberSequenceName = "PolicyNumberSequence";

    /// <summary>Nombre de la secuencia que alimenta el correlativo del certificado.</summary>
    public const string CertificateNumberSequenceName = "CertificateNumberSequence";

    public InsuranceDbContext(DbContextOptions<InsuranceDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<Coverage> Coverages => Set<Coverage>();

    public DbSet<Policy> Policies => Set<Policy>();

    public DbSet<PolicyCoverage> PolicyCoverages => Set<PolicyCoverage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasSequence<int>(PolicyNumberSequenceName)
            .StartsAt(1)
            .IncrementsBy(1);

        modelBuilder.HasSequence<int>(CertificateNumberSequenceName)
            .StartsAt(1)
            .IncrementsBy(1);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InsuranceDbContext).Assembly);
    }
}
