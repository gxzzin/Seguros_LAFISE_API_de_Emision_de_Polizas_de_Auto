namespace Lafise.Insurance.Application.Abstractions;

/// <summary>
/// Genera el número de póliza correlativo con el formato de Seguros LAFISE
/// (por ejemplo <c>AU-1234567-000123-0</c>). Se abstrae porque la implementación
/// depende del motor de base de datos (secuencias de SQL Server).
/// </summary>
public interface IPolicyNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
