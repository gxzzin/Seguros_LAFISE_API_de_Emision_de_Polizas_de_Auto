using Lafise.Insurance.Application.Contracts.Policies;
using Lafise.Insurance.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lafise.Insurance.Api.Controllers;

/// <summary>
/// Emisión y consulta de pólizas de automóvil. El controlador sólo orquesta la
/// petición HTTP: la validación de negocio y el cálculo viven en <see cref="IPolicyService"/>.
/// </summary>
[ApiController]
[Route("api/polizas")]
[Produces("application/json")]
public class PoliciesController : ControllerBase
{
    private readonly IPolicyService _policyService;

    public PoliciesController(IPolicyService policyService) => _policyService = policyService;

    /// <summary>Emite una nueva póliza y calcula la prima total en el servidor.</summary>
    /// <response code="201">Póliza emitida correctamente.</response>
    /// <response code="400">Datos inválidos o regla de negocio incumplida (antigüedad, placa, coberturas).</response>
    /// <response code="404">El cliente o alguna cobertura no existe.</response>
    /// <response code="409">El cliente ya tiene una póliza activa para esa placa.</response>
    [HttpPost("emitir")]
    [ProducesResponseType(typeof(PolicyDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PolicyDetailDto>> Issue(
        [FromBody] IssuePolicyRequest request,
        CancellationToken cancellationToken)
    {
        var policy = await _policyService.IssueAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = policy.Id }, policy);
    }

    /// <summary>Obtiene el detalle de una póliza emitida.</summary>
    /// <response code="200">Detalle de la póliza.</response>
    /// <response code="404">No existe una póliza con ese identificador.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PolicyDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PolicyDetailDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _policyService.GetByIdAsync(id, cancellationToken));

    /// <summary>Historial de todas las emisiones, de la más reciente a la más antigua.</summary>
    /// <response code="200">Listado de pólizas emitidas.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PolicySummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PolicySummaryDto>>> GetHistory(CancellationToken cancellationToken) =>
        Ok(await _policyService.GetHistoryAsync(cancellationToken));
}
