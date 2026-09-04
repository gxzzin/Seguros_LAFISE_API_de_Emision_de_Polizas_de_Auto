using Lafise.Insurance.Application.Abstractions;

namespace Lafise.Insurance.Infrastructure.Services;

/// <summary>Reloj real del sistema.</summary>
public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
