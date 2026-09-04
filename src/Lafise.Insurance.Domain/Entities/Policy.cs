using Lafise.Insurance.Domain.Enums;

namespace Lafise.Insurance.Domain.Entities;

/// <summary>
/// Póliza de seguro de automóvil emitida para un cliente y un vehículo.
/// </summary>
public class Policy
{
    public int Id { get; set; }

    /// <summary>Número de póliza autogenerado con formato POL-{año}-{consecutivo}.</summary>
    public string PolicyNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    public DateTime IssueDate { get; set; }

    /// <summary>Fecha de fin de vigencia (por defecto un año después de la emisión).</summary>
    public DateTime ExpirationDate { get; set; }

    /// <summary>Suma asegurada; corresponde al valor comercial del vehículo al momento de la emisión.</summary>
    public decimal InsuredAmount { get; set; }

    /// <summary>Prima total a pagar, calculada en el servidor a partir de las tasas de las coberturas.</summary>
    public decimal TotalPremium { get; set; }

    public PolicyStatus Status { get; set; } = PolicyStatus.Active;

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<PolicyCoverage> Coverages { get; set; } = new List<PolicyCoverage>();
}
