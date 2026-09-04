using System.Data;
using Lafise.Insurance.Application.Abstractions;
using Lafise.Insurance.Application.Options;
using Lafise.Insurance.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lafise.Insurance.Infrastructure.Services;

/// <summary>
/// Genera el número de póliza con el formato de Seguros LAFISE. El valor del ejemplo
/// es ficticio (<c>AU-1234567-000123-0</c>):
///
/// <code>
///   {ramo}-{correlativo de póliza:7}-{correlativo de certificado:6}-{endoso:1}
/// </code>
///
/// Los correlativos salen de dos secuencias de SQL Server: delegarlos al motor evita
/// colisiones cuando hay emisiones concurrentes.
/// </summary>
public class SequencePolicyNumberGenerator : IPolicyNumberGenerator
{
    private const int PolicyDigits = 7;
    private const int CertificateDigits = 6;

    /// <summary>Toda emisión nace como endoso 0; las renovaciones y modificaciones lo incrementarían.</summary>
    private const int OriginalEndorsement = 0;

    private readonly InsuranceDbContext _context;
    private readonly PolicyNumberingOptions _options;

    public SequencePolicyNumberGenerator(
        InsuranceDbContext context,
        IOptions<PolicyNumberingOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        // Se usan parámetros de salida en lugar de un SELECT: SQL Server no admite
        // NEXT VALUE FOR dentro de subconsultas, y así lo envuelve EF al proyectar un escalar.
        // Las dos secuencias se piden en una sola ida a la base de datos.
        var policySequence = CreateOutputParameter("@policySequence");
        var certificateSequence = CreateOutputParameter("@certificateSequence");

        await _context.Database.ExecuteSqlRawAsync(
            $"SET @policySequence = NEXT VALUE FOR [{InsuranceDbContext.PolicyNumberSequenceName}];" +
            $"SET @certificateSequence = NEXT VALUE FOR [{InsuranceDbContext.CertificateNumberSequenceName}];",
            new[] { policySequence, certificateSequence },
            cancellationToken);

        var policyNumber = (int)policySequence.Value;
        var certificateNumber = (int)certificateSequence.Value;

        return string.Join('-',
            _options.BranchCode,
            policyNumber.ToString($"D{PolicyDigits}"),
            certificateNumber.ToString($"D{CertificateDigits}"),
            OriginalEndorsement);
    }

    private static SqlParameter CreateOutputParameter(string name) =>
        new(name, SqlDbType.Int) { Direction = ParameterDirection.Output };
}
