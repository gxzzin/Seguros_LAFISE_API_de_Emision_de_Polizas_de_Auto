namespace Lafise.Insurance.Domain.Exceptions;

/// <summary>
/// Se lanza cuando la operación es sintácticamente válida pero incumple una regla
/// de negocio. La API la traduce a HTTP 400 (o 409 si el conflicto es de estado).
/// </summary>
public class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string code, string message) : base(message)
    {
        Code = code;
    }

    /// <summary>Código estable de la regla incumplida, útil para el cliente de la API.</summary>
    public string Code { get; }
}
