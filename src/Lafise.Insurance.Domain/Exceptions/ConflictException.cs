namespace Lafise.Insurance.Domain.Exceptions;

/// <summary>
/// Conflicto con el estado actual de los datos (por ejemplo, una póliza activa duplicada).
/// La API la traduce a HTTP 409.
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
