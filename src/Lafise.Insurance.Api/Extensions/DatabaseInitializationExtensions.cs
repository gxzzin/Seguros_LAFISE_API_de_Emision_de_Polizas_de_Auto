using Lafise.Insurance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lafise.Insurance.Api.Extensions;

/// <summary>
/// Aplica las migraciones pendientes al arrancar. Es cómodo para una prueba técnica
/// o un entorno de desarrollo; en producción las migraciones se ejecutan en el despliegue.
/// </summary>
public static class DatabaseInitializationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var context = scope.ServiceProvider.GetRequiredService<InsuranceDbContext>();

        try
        {
            logger.LogInformation("Aplicando migraciones pendientes de la base de datos...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Base de datos actualizada.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No fue posible aplicar las migraciones. Revise la cadena de conexión.");
            throw;
        }
    }
}
