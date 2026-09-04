namespace Lafise.Insurance.Domain.Exceptions;

/// <summary>
/// Excepción base para los errores originados por las reglas del dominio.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
