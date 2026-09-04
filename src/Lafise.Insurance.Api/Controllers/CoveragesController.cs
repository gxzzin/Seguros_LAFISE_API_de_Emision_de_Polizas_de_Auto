using Lafise.Insurance.Application.Contracts.Coverages;
using Lafise.Insurance.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lafise.Insurance.Api.Controllers;

/// <summary>CRUD del catálogo de coberturas.</summary>
[ApiController]
[Route("api/coberturas")]
[Produces("application/json")]
public class CoveragesController : ControllerBase
{
    private readonly ICoverageService _coverageService;

    public CoveragesController(ICoverageService coverageService) => _coverageService = coverageService;

    /// <summary>Lista las coberturas del catálogo.</summary>
    /// <param name="onlyActive">Si es true (valor por defecto) devuelve sólo las coberturas vigentes.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <response code="200">Listado de coberturas.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CoverageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CoverageDto>>> GetAll(
        [FromQuery] bool onlyActive = true,
        CancellationToken cancellationToken = default) =>
        Ok(await _coverageService.GetAllAsync(onlyActive, cancellationToken));

    /// <summary>Obtiene una cobertura por su identificador.</summary>
    /// <response code="200">Cobertura encontrada.</response>
    /// <response code="404">No existe una cobertura con ese identificador.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CoverageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CoverageDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _coverageService.GetByIdAsync(id, cancellationToken));

    /// <summary>Da de alta una cobertura.</summary>
    /// <response code="201">Cobertura creada.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="409">Ya existe una cobertura con el mismo nombre.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CoverageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CoverageDto>> Create(
        [FromBody] CreateCoverageRequest request,
        CancellationToken cancellationToken)
    {
        var coverage = await _coverageService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = coverage.Id }, coverage);
    }

    /// <summary>
    /// Actualiza una cobertura. Cambiar la tasa no afecta a las pólizas ya emitidas: cada una
    /// conserva la tasa con la que se calculó su prima.
    /// </summary>
    /// <response code="200">Cobertura actualizada.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="404">No existe una cobertura con ese identificador.</response>
    /// <response code="409">El nombre pertenece a otra cobertura.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CoverageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CoverageDto>> Update(
        int id,
        [FromBody] UpdateCoverageRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _coverageService.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// Descontinúa la cobertura (baja lógica): deja de ofrecerse en nuevas emisiones pero
    /// sigue visible en las pólizas históricas. Es idempotente.
    /// </summary>
    /// <response code="204">Cobertura descontinuada.</response>
    /// <response code="404">No existe una cobertura con ese identificador.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await _coverageService.DeactivateAsync(id, cancellationToken);

        return NoContent();
    }
}
