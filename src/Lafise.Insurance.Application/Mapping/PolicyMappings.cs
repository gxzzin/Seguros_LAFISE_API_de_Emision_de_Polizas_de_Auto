using Lafise.Insurance.Application.Contracts.Coverages;
using Lafise.Insurance.Application.Contracts.Customers;
using Lafise.Insurance.Application.Contracts.Policies;
using Lafise.Insurance.Application.Contracts.Vehicles;
using Lafise.Insurance.Domain.Entities;

namespace Lafise.Insurance.Application.Mapping;

/// <summary>
/// Conversión de entidades del dominio a contratos de salida. Se mantiene manual
/// para no acoplar el proyecto a una librería de mapeo.
/// </summary>
public static class PolicyMappings
{
    public static CustomerDto ToDto(this Customer customer) =>
        new(customer.Id, customer.Name, customer.IdentificationNumber, customer.Email, customer.IsActive);

    public static CoverageDto ToDto(this Coverage coverage) =>
        new(coverage.Id, coverage.Name, coverage.Description, coverage.Rate, coverage.IsActive);

    public static VehicleDto ToDto(this Vehicle vehicle) =>
        new(
            vehicle.Id,
            vehicle.Plate,
            vehicle.Brand,
            vehicle.Model,
            vehicle.Year,
            vehicle.CommercialValue,
            vehicle.IsActive);

    public static PolicySummaryDto ToSummaryDto(this Policy policy) =>
        new(
            policy.Id,
            policy.PolicyNumber,
            policy.Status.ToString(),
            policy.IssueDate,
            policy.Customer.Name,
            policy.Vehicle.Plate,
            policy.InsuredAmount,
            policy.TotalPremium);

    public static PolicyDetailDto ToDetailDto(this Policy policy) =>
        new(
            policy.Id,
            policy.PolicyNumber,
            policy.Status.ToString(),
            policy.IssueDate,
            policy.ExpirationDate,
            policy.InsuredAmount,
            policy.TotalPremium,
            new PolicyCustomerDto(
                policy.Customer.Id,
                policy.Customer.Name,
                policy.Customer.IdentificationNumber,
                policy.Customer.Email),
            new PolicyVehicleDto(
                policy.Vehicle.Id,
                policy.Vehicle.Plate,
                policy.Vehicle.Brand,
                policy.Vehicle.Model,
                policy.Vehicle.Year,
                policy.Vehicle.CommercialValue),
            policy.Coverages
                .Select(pc => new PolicyCoverageDto(
                    pc.CoverageId,
                    pc.Coverage?.Name ?? string.Empty,
                    pc.AppliedRate,
                    pc.PremiumAmount))
                .ToList());
}
