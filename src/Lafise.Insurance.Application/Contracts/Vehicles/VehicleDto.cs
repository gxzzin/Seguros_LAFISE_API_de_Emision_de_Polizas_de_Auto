namespace Lafise.Insurance.Application.Contracts.Vehicles;

/// <summary>Vehículo tal como se expone en el catálogo.</summary>
public record VehicleDto(
    int Id,
    string Plate,
    string Brand,
    string Model,
    int Year,
    decimal CommercialValue,
    bool IsActive);
