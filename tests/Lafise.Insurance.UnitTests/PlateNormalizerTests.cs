using Lafise.Insurance.Application.Services;
using Xunit;

namespace Lafise.Insurance.UnitTests;

/// <summary>Pruebas de la normalización de placas.</summary>
public class PlateNormalizerTests
{
    [Theory]
    [InlineData("M 105432", "M105432")] // Formato nicaragüense: letra departamental + 6 dígitos.
    [InlineData("m123456", "M123456")]
    [InlineData("ABC-1234", "ABC1234")]
    [InlineData(" ab 123 ", "AB123")]
    [InlineData("M.123.456", "M123456")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void Normalize_RemovesSeparatorsAndUppercases(string? input, string expected) =>
        Assert.Equal(expected, PlateNormalizer.Normalize(input));
}
