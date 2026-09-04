namespace Lafise.Insurance.Domain.Exceptions;

/// <summary>
/// Se lanza cuando una entidad referenciada no existe. La API la traduce a HTTP 404.
/// </summary>
public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object key)
        : this(entityName, key, $"No se encontró {entityName} con identificador '{key}'.")
    {
    }

    /// <summary>Permite redactar un mensaje propio cuando falta más de una entidad.</summary>
    public EntityNotFoundException(string entityName, object key, string message) : base(message)
    {
        EntityName = entityName;
        Key = key;
    }

    public string EntityName { get; }

    public object Key { get; }
}
