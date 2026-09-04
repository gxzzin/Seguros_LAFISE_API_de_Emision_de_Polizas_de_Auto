using Lafise.Insurance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lafise.Insurance.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de la tabla puente PolicyCoverages.</summary>
public class PolicyCoverageConfiguration : IEntityTypeConfiguration<PolicyCoverage>
{
    public void Configure(EntityTypeBuilder<PolicyCoverage> builder)
    {
        builder.ToTable("PolicyCoverages", table =>
        {
            table.HasCheckConstraint("CK_PolicyCoverages_AppliedRate", "[AppliedRate] >= 0 AND [AppliedRate] <= 100");
            table.HasCheckConstraint("CK_PolicyCoverages_PremiumAmount", "[PremiumAmount] >= 0");
        });

        // Clave primaria compuesta: una cobertura no puede repetirse dentro de la misma póliza.
        builder.HasKey(policyCoverage => new { policyCoverage.PolicyId, policyCoverage.CoverageId });

        builder.Property(policyCoverage => policyCoverage.AppliedRate)
            .HasPrecision(5, 2);

        builder.Property(policyCoverage => policyCoverage.PremiumAmount)
            .HasPrecision(18, 2);

        builder.HasOne(policyCoverage => policyCoverage.Policy)
            .WithMany(policy => policy.Coverages)
            .HasForeignKey(policyCoverage => policyCoverage.PolicyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(policyCoverage => policyCoverage.Coverage)
            .WithMany(coverage => coverage.PolicyCoverages)
            .HasForeignKey(policyCoverage => policyCoverage.CoverageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
