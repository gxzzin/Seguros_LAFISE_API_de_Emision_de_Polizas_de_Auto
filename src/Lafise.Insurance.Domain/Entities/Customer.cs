namespace Lafise.Insurance.Domain.Entities;

/// <summary>
/// Cliente (tomador) al que se le emiten las pólizas.
/// </summary>
public class Customer
{
    public int Id { get; set; }

    /// <summary>Nombre completo o razón social del cliente.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Identificación única del cliente (cédula/DNI/RUC).</summary>
    public string IdentificationNumber { get; set; } = string.Empty;

    /// <summary>Correo electrónico de contacto.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Indica si el cliente está habilitado. El borrado es lógico: un cliente con pólizas
    /// en el historial nunca se elimina físicamente.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<Policy> Policies { get; set; } = new List<Policy>();
}
