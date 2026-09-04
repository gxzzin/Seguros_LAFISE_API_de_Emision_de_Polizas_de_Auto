using Lafise.Insurance.Domain.Entities;
using Lafise.Insurance.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lafise.Insurance.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de la tabla Policies.</summary>
public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("Policies", table =>
        {
            table.HasCheckConstraint("CK_Policies_InsuredAmount", "[InsuredAmount] > 0");
            table.HasCheckConstraint("CK_Policies_TotalPremium", "[TotalPremium] >= 0");
            table.HasCheckConstraint("CK_Policies_Dates", "[ExpirationDate] > [IssueDate]");
        });

        builder.HasKey(policy => policy.Id);

        // AU-1234567-000123-0 son 19 caracteres; se deja holgura para endosos de más de un dígito.
        builder.Property(policy => policy.PolicyNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(policy => policy.InsuredAmount)
            .HasPrecision(18, 2);

        builder.Property(policy => policy.TotalPremium)
            .HasPrecision(18, 2);

        // El enum se persiste como tinyint para mantener el esquema compacto y legible.
        // El centinela 0 (valor por defecto del CLR) indica a EF cuándo debe delegar
        // el valor a la restricción DEFAULT de la tabla.
        builder.Property(policy => policy.Status)
            .HasConversion<byte>()
            .HasDefaultValue(PolicyStatus.Active)
            .HasSentinel(default);

        builder.Property(policy => policy.CreatedAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(policy => policy.PolicyNumber)
            .IsUnique()
            .HasDatabaseName("UX_Policies_PolicyNumber");

        // Regla de negocio respaldada por la base de datos: un cliente no puede tener
        // dos pólizas activas para el mismo vehículo (misma placa).
        builder.HasIndex(policy => new { policy.CustomerId, policy.VehicleId })
            .IsUnique()
            .HasFilter("[Status] = 1")
            .HasDatabaseName("UX_Policies_ActiveCustomerVehicle");

        builder.HasIndex(policy => policy.IssueDate)
            .HasDatabaseName("IX_Policies_IssueDate");

        builder.HasOne(policy => policy.Customer)
            .WithMany(customer => customer.Policies)
            .HasForeignKey(policy => policy.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(policy => policy.Vehicle)
            .WithMany(vehicle => vehicle.Policies)
            .HasForeignKey(policy => policy.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
