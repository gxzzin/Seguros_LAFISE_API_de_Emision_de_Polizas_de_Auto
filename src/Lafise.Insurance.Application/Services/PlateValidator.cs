using System.Text.RegularExpressions;
using Lafise.Insurance.Application.Options;
using Lafise.Insurance.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace Lafise.Insurance.Application.Services;

/// <summary>
/// Normaliza y valida el formato de la placa. Se extrae en su propio componente porque la
/// regla la comparten la emisión de pólizas y el mantenimiento del catálogo de vehículos.
/// </summary>
public interface IPlateValidator
{
    /// <summary>
    /// Devuelve la placa normalizada o lanza <see cref="BusinessRuleViolationException"/>
    /// si está vacía o no cumple el formato configurado.
    /// </summary>
    string NormalizeAndValidate(string? plate);
}

/// <inheritdoc cref="IPlateValidator"/>
public class PlateValidator : IPlateValidator
{
    private readonly Regex _plateRegex;

    public PlateValidator(IOptions<UnderwritingOptions> options) =>
        _plateRegex = new Regex(options.Value.PlatePattern, RegexOptions.CultureInvariant);

    public string NormalizeAndValidate(string? plate)
    {
        var normalizedPlate = PlateNormalizer.Normalize(plate);

        if (string.IsNullOrWhiteSpace(normalizedPlate))
        {
            throw new BusinessRuleViolationException("PLATE_REQUIRED", "La placa del vehículo es obligatoria.");
        }

        if (!_plateRegex.IsMatch(normalizedPlate))
        {
            throw new BusinessRuleViolationException(
                "INVALID_PLATE_FORMAT",
                $"La placa {normalizedPlate} no tiene un formato válido. Se esperan de 1 a 3 letras seguidas de 3 a 6 dígitos (ejemplo: M123456).");
        }

        return normalizedPlate;
    }
}
