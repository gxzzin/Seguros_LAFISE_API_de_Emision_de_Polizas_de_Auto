namespace Lafise.Insurance.Application.Abstractions;

/// <summary>
/// Abstrae el reloj del sistema para que las reglas de negocio sean verificables en pruebas.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
