using Lafise.Insurance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lafise.Insurance.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de la tabla Customers.</summary>
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(customer => customer.IdentificationNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(customer => customer.Email)
            .HasMaxLength(150)
            .IsRequired();

        // Sin DEFAULT en la columna: el valor por defecto del CLR para bool es false, así que
        // un DEFAULT en base de datos impediría insertar clientes inactivos (EF interpretaría
        // false como "sin asignar"). El valor inicial lo fija la entidad.
        builder.Property(customer => customer.IsActive)
            .IsRequired();

        builder.Property(customer => customer.CreatedAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // La identificación (cédula/DNI/RUC) no puede repetirse entre clientes.
        builder.HasIndex(customer => customer.IdentificationNumber)
            .IsUnique()
            .HasDatabaseName("UX_Customers_IdentificationNumber");

        // Clientes de ejemplo para poder probar la emisión inmediatamente después de migrar.
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new Customer
            {
                Id = 1,
                Name = "Maria Fernanda Lopez",
                IdentificationNumber = "001-120589-1002B",
                Email = "maria.lopez@example.com",
                IsActive = true,
                CreatedAtUtc = seedDate
            },
            new Customer
            {
                Id = 2,
                Name = "Carlos Alberto Mendoza",
                IdentificationNumber = "281-030777-0005X",
                Email = "carlos.mendoza@example.com",
                IsActive = true,
                CreatedAtUtc = seedDate
            },
            new Customer
            {
                Id = 3,
                Name = "Distribuidora El Norte S.A.",
                IdentificationNumber = "J0310000234567",
                Email = "compras@elnorte.example.com",
                IsActive = true,
                CreatedAtUtc = seedDate
            });
    }
}
