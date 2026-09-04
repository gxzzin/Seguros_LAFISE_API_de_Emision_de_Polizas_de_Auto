namespace Lafise.Insurance.Application.Options;

/// <summary>
/// Parámetros del número de póliza. El formato replica el de las pólizas de
/// Seguros LAFISE; el valor del ejemplo es ficticio:
///
/// <code>
///   AU   - 1234567 - 000123 - 0
///   │      │         │        └─ endoso: 0 en la emisión original
///   │      │         └────────── correlativo del certificado (6 dígitos)
///   │      └──────────────────── correlativo de la póliza (7 dígitos)
///   └─────────────────────────── código del ramo (AU = Automóvil)
/// </code>
/// </summary>
public class PolicyNumberingOptions
{
    public const string SectionName = "PolicyNumbering";

    /// <summary>Código del ramo que encabeza el número de póliza.</summary>
    public string BranchCode { get; set; } = "AU";
}
