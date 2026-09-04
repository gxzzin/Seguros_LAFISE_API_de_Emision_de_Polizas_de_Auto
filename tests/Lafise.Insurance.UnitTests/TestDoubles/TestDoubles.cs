using Lafise.Insurance.Application.Abstractions;

namespace Lafise.Insurance.UnitTests.TestDoubles;

/// <summary>Reloj fijo para que las reglas dependientes de la fecha sean deterministas.</summary>
public class FixedDateTimeProvider : IDateTimeProvider
{
    public FixedDateTimeProvider(DateTime utcNow) => UtcNow = utcNow;

    public DateTime UtcNow { get; }
}

/// <summary>
/// Generador de números de póliza en memoria; sustituye a las secuencias de SQL Server
/// reproduciendo el mismo formato real: AU-{póliza:7}-{certificado:6}-{endoso}.
/// </summary>
public class InMemoryPolicyNumberGenerator : IPolicyNumberGenerator
{
    private int _counter;

    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        _counter++;
        return Task.FromResult($"AU-{_counter:D7}-{_counter:D6}-0");
    }
}
