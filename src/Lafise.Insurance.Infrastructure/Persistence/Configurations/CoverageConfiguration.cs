using Lafise.Insurance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lafise.Insurance.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo y datos semilla de la tabla Coverages.</summary>
public class CoverageConfiguration : IEntityTypeConfiguration<Coverage>
{
    public void Configure(EntityTypeBuilder<Coverage> builder)
    {
        builder.ToTable("Coverages", table =>
            table.HasCheckConstraint("CK_Coverages_Rate", "[Rate] >= 0 AND [Rate] <= 100"));

        builder.HasKey(coverage => coverage.Id);

        builder.Property(coverage => coverage.Name)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(coverage => coverage.Description)
            .HasMaxLength(250);

        // Tasa en porcentaje: 2.50 = 2.5% de la suma asegurada.
        builder.Property(coverage => coverage.Rate)
            .HasPrecision(5, 2);

        // Sin DEFAULT en la columna a propósito: como el valor por defecto del CLR para bool
        // es false, un DEFAULT en base de datos impediría insertar coberturas inactivas
        // (EF interpretaría false como "sin asignar"). El valor inicial lo fija la entidad.
        builder.Property(coverage => coverage.IsActive)
            .IsRequired();

        builder.HasIndex(coverage => coverage.Name)
            .IsUnique()
            .HasDatabaseName("UX_Coverages_Name");

        builder.HasData(
            new Coverage
            {
                Id = 1,
                Name = "Robo",
                Description = "Cubre el hurto total del vehículo asegurado.",
                Rate = 2.50m,
                IsActive = true
            },
            new Coverage
            {
                Id = 2,
                Name = "Choque",
                Description = "Cubre daños propios por colisión o vuelco.",
                Rate = 3.25m,
                IsActive = true
            },
            new Coverage
            {
                Id = 3,
                Name = "Responsabilidad Civil",
                Description = "Cubre daños a terceros en sus bienes y personas.",
                Rate = 1.75m,
                IsActive = true
            },
            new Coverage
            {
                Id = 4,
                Name = "Incendio",
                Description = "Cubre daños por incendio, rayo o explosión.",
                Rate = 1.10m,
                IsActive = true
            },
            new Coverage
            {
                Id = 5,
                Name = "Rotura de Cristales",
                Description = "Cubre la rotura de parabrisas y ventanas.",
                Rate = 0.60m,
                IsActive = true
            },
            new Coverage
            {
                Id = 6,
                Name = "Asistencia Vial",
                Description = "Cobertura descontinuada; se conserva sólo para pólizas históricas.",
                Rate = 0.40m,
                IsActive = false
            });
    }
}
