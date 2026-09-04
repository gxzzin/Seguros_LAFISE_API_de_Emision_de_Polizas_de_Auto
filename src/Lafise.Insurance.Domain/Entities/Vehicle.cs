namespace Lafise.Insurance.Domain.Entities;

/// <summary>
/// Vehículo asegurado. Se registra al momento de la emisión si la placa aún no existe.
/// </summary>
public class Vehicle
{
    public int Id { get; set; }

    /// <summary>Placa del vehículo. Se almacena normalizada (mayúsculas, sin espacios) y es única.</summary>
    public string Plate { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    /// <summary>Año de fabricación.</summary>
    public int Year { get; set; }

    /// <summary>Valor comercial en moneda local; base para el cálculo de la prima.</summary>
    public decimal CommercialValue { get; set; }

    /// <summary>
    /// Indica si el vehículo está habilitado. El borrado es lógico para no perder las
    /// pólizas que lo referencian.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<Policy> Policies { get; set; } = new List<Policy>();
}
