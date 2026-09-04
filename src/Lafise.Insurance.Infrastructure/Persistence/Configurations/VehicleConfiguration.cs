using Lafise.Insurance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lafise.Insurance.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de la tabla Vehicles.</summary>
public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles", table =>
        {
            table.HasCheckConstraint("CK_Vehicles_Year", "[Year] BETWEEN 1900 AND 2200");
            table.HasCheckConstraint("CK_Vehicles_CommercialValue", "[CommercialValue] > 0");
        });

        builder.HasKey(vehicle => vehicle.Id);

        builder.Property(vehicle => vehicle.Plate)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(vehicle => vehicle.Brand)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(vehicle => vehicle.Model)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(vehicle => vehicle.CommercialValue)
            .HasPrecision(18, 2);

        // Sin DEFAULT en la columna, por el mismo motivo documentado en CustomerConfiguration.
        builder.Property(vehicle => vehicle.IsActive)
            .IsRequired();

        builder.Property(vehicle => vehicle.CreatedAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // La placa se almacena normalizada y es la clave natural del vehículo.
        builder.HasIndex(vehicle => vehicle.Plate)
            .IsUnique()
            .HasDatabaseName("UX_Vehicles_Plate");
    }
}
