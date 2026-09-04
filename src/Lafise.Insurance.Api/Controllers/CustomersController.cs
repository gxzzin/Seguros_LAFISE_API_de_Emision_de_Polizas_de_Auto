using Lafise.Insurance.Application.Contracts.Customers;
using Lafise.Insurance.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lafise.Insurance.Api.Controllers;

/// <summary>CRUD del catálogo de clientes.</summary>
[ApiController]
[Route("api/clientes")]
[Produces("application/json")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService) => _customerService = customerService;

    /// <summary>Lista los clientes registrados.</summary>
    /// <param name="onlyActive">Si es true (valor por defecto) omite los clientes dados de baja.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <response code="200">Listado de clientes.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> GetAll(
        [FromQuery] bool onlyActive = true,
        CancellationToken cancellationToken = default) =>
        Ok(await _customerService.GetAllAsync(onlyActive, cancellationToken));

    /// <summary>Obtiene un cliente por su identificador.</summary>
    /// <response code="200">Cliente encontrado.</response>
    /// <response code="404">No existe un cliente con ese identificador.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _customerService.GetByIdAsync(id, cancellationToken));

    /// <summary>Registra un nuevo cliente.</summary>
    /// <response code="201">Cliente creado.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="409">Ya existe un cliente con la misma identificación.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerDto>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await _customerService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    /// <summary>Actualiza los datos de un cliente.</summary>
    /// <response code="200">Cliente actualizado.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="404">No existe un cliente con ese identificador.</response>
    /// <response code="409">La identificación pertenece a otro cliente, o se intenta dar de baja un cliente con pólizas activas.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerDto>> Update(
        int id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _customerService.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// Da de baja al cliente. La baja es lógica: la fila se conserva para no perder el
    /// historial de pólizas. Es idempotente.
    /// </summary>
    /// <response code="204">Cliente dado de baja.</response>
    /// <response code="404">No existe un cliente con ese identificador.</response>
    /// <response code="409">El cliente tiene pólizas activas.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await _customerService.DeactivateAsync(id, cancellationToken);

        return NoContent();
    }
}
