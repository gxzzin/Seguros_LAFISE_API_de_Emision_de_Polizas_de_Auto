namespace Lafise.Insurance.Application.Contracts.Policies;

/// <summary>Detalle completo de una póliza emitida.</summary>
public record PolicyDetailDto(
    int Id,
    string PolicyNumber,
    string Status,
    DateTime IssueDate,
    DateTime ExpirationDate,
    decimal InsuredAmount,
    decimal TotalPremium,
    PolicyCustomerDto Customer,
    PolicyVehicleDto Vehicle,
    IReadOnlyList<PolicyCoverageDto> Coverages);

/// <summary>Cliente asociado a la póliza.</summary>
public record PolicyCustomerDto(int Id, string Name, string IdentificationNumber, string Email);

/// <summary>Vehículo asegurado por la póliza.</summary>
public record PolicyVehicleDto(int Id, string Plate, string Brand, string Model, int Year, decimal CommercialValue);

/// <summary>Cobertura aplicada, con la tasa y el monto congelados en la emisión.</summary>
public record PolicyCoverageDto(int CoverageId, string Name, decimal AppliedRate, decimal PremiumAmount);
