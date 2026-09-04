using System.Text.RegularExpressions;

namespace Lafise.Insurance.Application.Services;

/// <summary>
/// Normaliza la placa antes de validarla o compararla: mayúsculas y sin espacios,
/// guiones ni puntos. Así "abc-123" y "ABC 123" se consideran la misma placa.
/// </summary>
public static partial class PlateNormalizer
{
    public static string Normalize(string? plate) =>
        string.IsNullOrWhiteSpace(plate)
            ? string.Empty
            : SeparatorsRegex().Replace(plate.Trim().ToUpperInvariant(), string.Empty);

    [GeneratedRegex(@"[\s\-\.]")]
    private static partial Regex SeparatorsRegex();
}
