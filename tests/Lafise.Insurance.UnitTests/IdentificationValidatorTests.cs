using Lafise.Insurance.Application.Options;
using Lafise.Insurance.Application.Services;
using Lafise.Insurance.Domain.Exceptions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lafise.Insurance.UnitTests;

/// <summary>
/// Pruebas de la validación de la identificación: cédula nicaragüense y RUC jurídico.
/// El patrón de la cédula sigue el de la librería de referencia @nerdify/dnic.
/// </summary>
public class IdentificationValidatorTests
{
    private readonly IdentificationValidator _validator =
        new(Options.Create(new IdentificationOptions()));

    [Theory]
    [InlineData("281-140891-0022V")]
    [InlineData("001-120589-1002B")]
    [InlineData("281-030777-0005X")] // X es la última letra de control admitida.
    [InlineData("601-010100-0001A")] // 6 es el primer dígito de municipio más alto admitido.
    public void NormalizeAndValidate_AcceptsAValidCedula(string identification) =>
        Assert.Equal(identification, _validator.NormalizeAndValidate(identification));

    [Theory]
    [InlineData("J0310000234567")]
    [InlineData("J0310000000999")]
    public void NormalizeAndValidate_AcceptsAJuridicalRuc(string identification) =>
        Assert.Equal(identification, _validator.NormalizeAndValidate(identification));

    [Theory]
    [InlineData("281-140891-0022v", "281-140891-0022V")]
    [InlineData("  281-140891-0022V  ", "281-140891-0022V")]
    [InlineData("j0310000234567", "J0310000234567")]
    public void NormalizeAndValidate_UppercasesAndTrims(string input, string expected) =>
        Assert.Equal(expected, _validator.NormalizeAndValidate(input));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void NormalizeAndValidate_WithoutIdentification_Throws(string? identification)
    {
        var exception = Assert.Throws<BusinessRuleViolationException>(
            () => _validator.NormalizeAndValidate(identification));

        Assert.Equal("IDENTIFICATION_REQUIRED", exception.Code);
    }

    [Theory]
    [InlineData("12345")]                 // Lo que la API aceptaba antes.
    [InlineData("281-140891-0022")]       // Sin letra de control.
    [InlineData("281-140891-0022Y")]      // Y queda fuera del rango A-X.
    [InlineData("281-140891-0022Z")]
    [InlineData("781-140891-0022V")]      // El municipio no puede empezar por 7.
    [InlineData("001-321294-0022V")]      // Día 32.
    [InlineData("001-041394-0022V")]      // Mes 13.
    [InlineData("281-1408911-0022V")]     // Fecha de siete dígitos.
    [InlineData("2811408910022V")]        // Sin guiones.
    [InlineData("J031000023456")]         // RUC con 12 dígitos.
    [InlineData("J03100002345678")]       // RUC con 14 dígitos.
    public void NormalizeAndValidate_WithAnInvalidFormat_Throws(string identification)
    {
        var exception = Assert.Throws<BusinessRuleViolationException>(
            () => _validator.NormalizeAndValidate(identification));

        Assert.Equal("INVALID_IDENTIFICATION_FORMAT", exception.Code);
    }

    [Theory]
    [InlineData("281-300294-0022V")] // 30 de febrero.
    [InlineData("001-310494-0022V")] // 31 de abril.
    [InlineData("001-000594-0022V")] // Día 00.
    public void NormalizeAndValidate_RejectsABirthDateThatDoesNotExist(string identification)
    {
        var exception = Assert.Throws<BusinessRuleViolationException>(
            () => _validator.NormalizeAndValidate(identification));

        Assert.Equal("INVALID_IDENTIFICATION_FORMAT", exception.Code);
    }

    [Fact]
    public void NormalizeAndValidate_AcceptsFebruary29OfALeapYear()
    {
        // El año viene con dos dígitos: 2000 fue bisiesto aunque 1900 no lo fue, así que
        // la fecha debe aceptarse en lugar de rechazar a quien nació ese día.
        Assert.Equal("001-290200-0022V", _validator.NormalizeAndValidate("001-290200-0022V"));
    }

    [Fact]
    public void NormalizeAndValidate_WhenBirthDateValidationIsDisabled_OnlyChecksTheShape()
    {
        var validator = new IdentificationValidator(
            Options.Create(new IdentificationOptions { ValidateBirthDate = false }));

        Assert.Equal("281-300294-0022V", validator.NormalizeAndValidate("281-300294-0022V"));
    }

    [Fact]
    public void NormalizeAndValidate_WithAWiderPattern_AcceptsLettersBeyondX()
    {
        // El rango A-X viene de la librería de referencia, pero el patrón es configurable
        // por si en la práctica aparecen letras fuera de ese rango.
        var validator = new IdentificationValidator(Options.Create(new IdentificationOptions
        {
            CedulaPattern = @"^[0-6]\d{2}-([0-2]\d|3[01])(0[1-9]|1[0-2])\d{2}-\d{4}[A-Z]$"
        }));

        Assert.Equal("021-210494-0000Y", validator.NormalizeAndValidate("021-210494-0000Y"));
    }
}
