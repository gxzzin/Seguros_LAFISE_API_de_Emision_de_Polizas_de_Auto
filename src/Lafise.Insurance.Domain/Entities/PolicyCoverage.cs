namespace Lafise.Insurance.Domain.Entities;

/// <summary>
/// Relación muchos a muchos entre póliza y cobertura. Guarda una "foto" de la tasa
/// y del monto de prima aplicados en la emisión, de modo que un cambio posterior en
/// el catálogo no altere las pólizas ya emitidas.
/// </summary>
public class PolicyCoverage
{
    public int PolicyId { get; set; }
    public Policy Policy { get; set; } = null!;

    public int CoverageId { get; set; }
    public Coverage Coverage { get; set; } = null!;

    /// <summary>Tasa vigente al momento de la emisión (porcentaje).</summary>
    public decimal AppliedRate { get; set; }

    /// <summary>Prima aportada por esta cobertura = SumaAsegurada * (AppliedRate / 100).</summary>
    public decimal PremiumAmount { get; set; }
}
