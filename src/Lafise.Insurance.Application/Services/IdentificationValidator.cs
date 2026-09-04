using System.Text.RegularExpressions;
using Lafise.Insurance.Application.Options;
using Lafise.Insurance.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace Lafise.Insurance.Application.Services;

/// <summary>
/// Normaliza y valida la identificación del cliente: cédula nicaragüense o RUC jurídico.
/// </summary>
public interface IIdentificationValidator
{
    /// <summary>
    /// Devuelve la identificación normalizada o lanza <see cref="BusinessRuleViolationException"/>
    /// si está vacía o no corresponde a ninguno de los dos formatos aceptados.
    /// </summary>
    string NormalizeAndValidate(string? identificationNumber);
}

/// <inheritdoc cref="IIdentificationValidator"/>
public class IdentificationValidator : IIdentificationValidator
{
    private readonly Regex _cedulaRegex;
    private readonly Regex _rucRegex;
    private readonly bool _validateBirthDate;

    public IdentificationValidator(IOptions<IdentificationOptions> options)
    {
        var value = options.Value;
        _cedulaRegex = new Regex(value.CedulaPattern, RegexOptions.CultureInvariant);
        _rucRegex = new Regex(value.RucPattern, RegexOptions.CultureInvariant);
        _validateBirthDate = value.ValidateBirthDate;
    }

    public string NormalizeAndValidate(string? identificationNumber)
    {
        // Se pasa a mayúsculas porque la letra de control se escribe indistintamente en
        // minúscula o mayúscula. Sin esto, '281-140891-0022v' y '281-140891-0022V' serían
        // dos clientes distintos y ambos pasarían el índice único.
        var normalized = (identificationNumber ?? string.Empty).Trim().ToUpperInvariant();

        if (normalized.Length == 0)
        {
            throw new BusinessRuleViolationException(
                "IDENTIFICATION_REQUIRED",
                "La identificación del cliente es obligatoria.");
        }

        if (_rucRegex.IsMatch(normalized))
        {
            return normalized;
        }

        if (_cedulaRegex.IsMatch(normalized) && HasValidBirthDate(normalized))
        {
            return normalized;
        }

        throw new BusinessRuleViolationException(
            "INVALID_IDENTIFICATION_FORMAT",
            $"La identificación '{normalized}' no tiene un formato válido. Se espera una cédula " +
            "nicaragüense (por ejemplo 281-140891-0022V) o un RUC jurídico (por ejemplo J0310000234567).");
    }

    /// <summary>
    /// Comprueba que la fecha embebida en la cédula exista. El año viene con dos dígitos, así
    /// que el siglo es ambiguo: se acepta si la fecha es válida en cualquiera de los dos. De lo
    /// contrario se rechazaría a alguien nacido el 29/02/2000, ya que 1900 no fue bisiesto.
    /// </summary>
    private bool HasValidBirthDate(string normalizedCedula)
    {
        if (!_validateBirthDate)
        {
            return true;
        }

        var birthDate = normalizedCedula.Split('-')[1];
        var day = int.Parse(birthDate[..2]);
        var month = int.Parse(birthDate.Substring(2, 2));
        var year = int.Parse(birthDate[4..]);

        return DateExists(1900 + year, month, day) || DateExists(2000 + year, month, day);
    }

    private static bool DateExists(int year, int month, int day) =>
        month is >= 1 and <= 12 && day >= 1 && day <= DateTime.DaysInMonth(year, month);
}
