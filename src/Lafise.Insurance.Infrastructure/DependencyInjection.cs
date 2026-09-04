using Lafise.Insurance.Application.Abstractions;
using Lafise.Insurance.Infrastructure.Persistence;
using Lafise.Insurance.Infrastructure.Persistence.Repositories;
using Lafise.Insurance.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lafise.Insurance.Infrastructure;

/// <summary>
/// Registro de la capa de infraestructura (EF Core, repositorios y servicios técnicos).
/// </summary>
public static class DependencyInjection
{
    public const string ConnectionStringName = "InsuranceDatabase";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"No se configuró la cadena de conexión '{ConnectionStringName}' en appsettings.json.");

        services.AddDbContext<InsuranceDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(InsuranceDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure();
            }));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<ICoverageRepository, CoverageRepository>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<IPolicyNumberGenerator, SequencePolicyNumberGenerator>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
