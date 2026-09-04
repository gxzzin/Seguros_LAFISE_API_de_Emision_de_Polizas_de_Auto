using Lafise.Insurance.Application.Options;
using Lafise.Insurance.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lafise.Insurance.Application;

/// <summary>
/// Registro de la capa de aplicación en el contenedor nativo de .NET.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<UnderwritingOptions>(configuration.GetSection(UnderwritingOptions.SectionName));
        services.Configure<PolicyNumberingOptions>(configuration.GetSection(PolicyNumberingOptions.SectionName));
        services.Configure<IdentificationOptions>(configuration.GetSection(IdentificationOptions.SectionName));

        // El validador de placas no tiene estado mutable y compila la expresión regular
        // una sola vez, así que se registra como Singleton.
        services.AddSingleton<IPlateValidator, PlateValidator>();
        services.AddSingleton<IIdentificationValidator, IdentificationValidator>();

        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICoverageService, CoverageService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IPolicyService, PolicyService>();

        return services;
    }
}
