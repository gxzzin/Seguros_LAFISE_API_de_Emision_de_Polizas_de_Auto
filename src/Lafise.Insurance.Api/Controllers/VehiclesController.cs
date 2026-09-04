using Lafise.Insurance.Application.Contracts.Vehicles;
using Lafise.Insurance.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lafise.Insurance.Api.Controllers;

/// <summary>
/// CRUD del catálogo de vehículos. La emisión también registra vehículos por su cuenta
/// cuando la placa aún no existe; estos endpoints permiten mantenerlos aparte.
/// </summary>
[ApiController]
[Route("api/vehiculos")]
[Produces("application/json")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService) => _vehicleService = vehicleService;

    /// <summary>Lista los vehículos registrados.</summary>
    /// <param name="onlyActive">Si es true (valor por defecto) omite los vehículos dados de baja.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <response code="200">Listado de vehículos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VehicleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VehicleDto>>> GetAll(
        [FromQuery] bool onlyActive = true,
        CancellationToken cancellationToken = default) =>
        Ok(await _vehicleService.GetAllAsync(onlyActive, cancellationToken));

    /// <summary>Obtiene un vehículo por su identificador.</summary>
    /// <response code="200">Vehículo encontrado.</response>
    /// <response code="404">No existe un vehículo con ese identificador.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _vehicleService.GetByIdAsync(id, cancellationToken));

    /// <summary>Registra un vehículo. La placa se normaliza y se valida su formato.</summary>
    /// <response code="201">Vehículo creado.</response>
    /// <response code="400">Datos inválidos o placa con formato incorrecto.</response>
    /// <response code="409">Ya existe un vehículo con la misma placa.</response>
    [HttpPost]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleDto>> Create(
        [FromBody] CreateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, vehicle);
    }

    /// <summary>Actualiza los datos de un vehículo.</summary>
    /// <response code="200">Vehículo actualizado.</response>
    /// <response code="400">Datos inválidos o placa con formato incorrecto.</response>
    /// <response code="404">No existe un vehículo con ese identificador.</response>
    /// <response code="409">La placa pertenece a otro vehículo, o se intenta dar de baja un vehículo con pólizas activas.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleDto>> Update(
        int id,
        [FromBody] UpdateVehicleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _vehicleService.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// Da de baja el vehículo. La baja es lógica para no perder las pólizas que lo
    /// referencian. Es idempotente.
    /// </summary>
    /// <response code="204">Vehículo dado de baja.</response>
    /// <response code="404">No existe un vehículo con ese identificador.</response>
    /// <response code="409">El vehículo tiene pólizas activas.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await _vehicleService.DeactivateAsync(id, cancellationToken);

        return NoContent();
    }
}
