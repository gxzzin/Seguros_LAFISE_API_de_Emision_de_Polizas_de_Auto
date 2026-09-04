namespace Lafise.Insurance.Application.Options;

/// <summary>
/// Patrones de la identificación del cliente. El enunciado pide aceptar "DNI/RUC", así que
/// se admiten dos formatos: la cédula de identidad nicaragüense (persona natural) y el RUC
/// jurídico (empresa).
/// </summary>
public class IdentificationOptions
{
    public const string SectionName = "Identification";

    /// <summary>
    /// Cédula de identidad nicaragüense, con el formato <c>NNN-DDMMAA-NNNNL</c>:
    ///
    /// <code>
    ///   281 - 140891 - 0022V
    ///   │     │        │  └─ letra de control (A-X)
    ///   │     │        └──── número de producción (4 dígitos)
    ///   │     └───────────── fecha de nacimiento (DDMMAA)
    ///   └─────────────────── código del municipio (el primer dígito va de 0 a 6)
    /// </code>
    ///
    /// El patrón está tomado de la librería de referencia <c>@nerdify/dnic</c>
    /// (https://github.com/nerdify/dnic, licencia ISC).
    /// </summary>
    public string CedulaPattern { get; set; } = @"^[0-6]\d{2}-([0-2]\d|3[01])(0[1-9]|1[0-2])\d{2}-\d{4}[A-X]$";

    /// <summary>
    /// RUC jurídico: una letra seguida de 13 dígitos (por ejemplo <c>J0310000234567</c>).
    /// </summary>
    public string RucPattern { get; set; } = @"^[A-Z]\d{13}$";

    /// <summary>
    /// Comprueba además que la fecha de nacimiento embebida en la cédula exista de verdad,
    /// de modo que <c>001-999999-0022V</c> se rechace aunque encaje en el patrón.
    /// </summary>
    public bool ValidateBirthDate { get; set; } = true;
}
