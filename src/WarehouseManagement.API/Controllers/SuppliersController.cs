using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.Suppliers.Commands.ActivateSupplier;
using WarehouseManagement.Application.Features.Suppliers.Commands.CreateSupplier;
using WarehouseManagement.Application.Features.Suppliers.Commands.DeleteSupplier;
using WarehouseManagement.Application.Features.Suppliers.Commands.UpdateSupplier;
using WarehouseManagement.Application.Features.Suppliers.DTOs;
using WarehouseManagement.Application.Features.Suppliers.Queries.GetSupplierById;
using WarehouseManagement.Application.Features.Suppliers.Queries.GetSuppliers;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly IMediator _mediator;

    public SuppliersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieve paginated list of external suppliers with search support.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<SupplierDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool includeInactive = false)
    {
        var result = await _mediator.Send(new GetSuppliersQuery(pageNumber, pageSize, searchTerm, includeInactive));
        return Ok(result);
    }

    /// <summary>
    /// Retrieve supplier details by unique ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetSupplierByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Register a new external product supplier / vendor.
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update supplier contact information and details.
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSupplierRequest request)
    {
        var command = new UpdateSupplierCommand(
            id,
            request.Name,
            request.Email,
            request.PhoneNumber,
            request.ContactPerson,
            request.Address);

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Soft delete / deactivate a supplier.
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediator.Send(new DeleteSupplierCommand(id));
        return NoContent();
    }

    /// <summary>
    /// Reactivate a previously deactivated supplier.
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpPost("{id:int}/activate")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Activate(int id)
    {
        await _mediator.Send(new ActivateSupplierCommand(id));
        return Ok(new { message = $"Supplier {id} reactivated successfully." });
    }
}

public record UpdateSupplierRequest(
    string Name,
    string Email,
    string PhoneNumber,
    string? ContactPerson = null,
    string? Address = null
);
