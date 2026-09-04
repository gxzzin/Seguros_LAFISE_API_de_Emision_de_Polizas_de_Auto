namespace Lafise.Insurance.Domain.Entities;

/// <summary>
/// Catálogo de coberturas disponibles (Robo, Choque, Responsabilidad Civil, etc.).
/// </summary>
public class Coverage
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Tasa de la cobertura expresada en porcentaje (ej.: 2.50 equivale a 2.5% del valor asegurado).
    /// </summary>
    public decimal Rate { get; set; }

    /// <summary>Indica si la cobertura puede seleccionarse en nuevas emisiones.</summary>
    public bool IsActive { get; set; } = true;

    public ICollection<PolicyCoverage> PolicyCoverages { get; set; } = new List<PolicyCoverage>();
}
