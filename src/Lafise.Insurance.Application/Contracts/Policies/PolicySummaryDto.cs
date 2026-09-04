namespace Lafise.Insurance.Application.Contracts.Policies;

/// <summary>Fila del historial de emisiones.</summary>
public record PolicySummaryDto(
    int Id,
    string PolicyNumber,
    string Status,
    DateTime IssueDate,
    string CustomerName,
    string Plate,
    decimal InsuredAmount,
    decimal TotalPremium);
