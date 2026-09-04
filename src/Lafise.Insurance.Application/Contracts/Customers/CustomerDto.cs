namespace Lafise.Insurance.Application.Contracts.Customers;

/// <summary>Cliente tal como se expone en el catálogo de la API.</summary>
public record CustomerDto(int Id, string Name, string IdentificationNumber, string Email, bool IsActive);
