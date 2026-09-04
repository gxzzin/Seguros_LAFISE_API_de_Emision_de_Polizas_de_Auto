namespace Lafise.Insurance.Application.Options;

/// <summary>
/// Parámetros de suscripción configurables desde appsettings; evitan "números mágicos"
/// dentro de la lógica de negocio.
/// </summary>
public class UnderwritingOptions
{
    public const string SectionName = "Underwriting";

    /// <summary>Antigüedad máxima permitida del vehículo, en años. Regla del enunciado: 20.</summary>
    public int MaxVehicleAgeInYears { get; set; } = 20;

    /// <summary>Expresión regular que valida el formato de la placa ya normalizada.</summary>
    public string PlatePattern { get; set; } = "^[A-Z]{1,3}[0-9]{3,6}$";

    /// <summary>Vigencia de la póliza en meses.</summary>
    public int PolicyTermInMonths { get; set; } = 12;

    /// <summary>Prima mínima a cobrar aunque el cálculo por tasas resulte inferior.</summary>
    public decimal MinimumPremium { get; set; } = 0m;
}
