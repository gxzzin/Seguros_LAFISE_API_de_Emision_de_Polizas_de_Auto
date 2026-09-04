namespace Lafise.Insurance.Application.Contracts.Coverages;

/// <summary>Cobertura disponible para la emisión.</summary>
public record CoverageDto(int Id, string Name, string? Description, decimal Rate, bool IsActive);
