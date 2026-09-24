using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.Inventory.DTOs;
using WarehouseManagement.Application.Features.Warehouses.Commands.ActivateWarehouse;
using WarehouseManagement.Application.Features.Warehouses.Commands.CreateWarehouse;
using WarehouseManagement.Application.Features.Warehouses.Commands.DeleteWarehouse;
using WarehouseManagement.Application.Features.Warehouses.Commands.UpdateWarehouse;
using WarehouseManagement.Application.Features.Warehouses.DTOs;
using WarehouseManagement.Application.Features.Warehouses.Queries.GetWarehouseById;
using WarehouseManagement.Application.Features.Warehouses.Queries.GetWarehouses;
using WarehouseManagement.Application.Features.Warehouses.Queries.GetWarehouseStock;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehousesController : ControllerBase
{
    private readonly IMediator _mediator;

    public WarehousesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieve all warehouses with active product counts and stock units.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<WarehouseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var result = await _mediator.Send(new GetWarehousesQuery(includeInactive));
        return Ok(result);
    }

    /// <summary>
    /// Retrieve a warehouse facility by its unique ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetWarehouseByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Retrieve paginated stock and inventory items located within this warehouse.
    /// </summary>
    [HttpGet("{id:int}/stock")]
    [ProducesResponseType(typeof(PaginatedList<InventoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStock(
        int id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null)
    {
        var result = await _mediator.Send(new GetWarehouseStockQuery(id, pageNumber, pageSize, searchTerm));
        return Ok(result);
    }

    /// <summary>
    /// Register a new warehouse facility.
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update warehouse facility details.
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWarehouseRequest request)
    {
        var command = new UpdateWarehouseCommand(id, request.Name, request.Location);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Deactivate a warehouse facility.
    /// Requires zero inventory on hand before deactivation.
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediator.Send(new DeleteWarehouseCommand(id));
        return NoContent();
    }

    /// <summary>
    /// Reactivate a previously deactivated warehouse facility.
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
        await _mediator.Send(new ActivateWarehouseCommand(id));
        return Ok(new { message = $"Warehouse {id} reactivated successfully." });
    }
}

public record UpdateWarehouseRequest(string Name, string Location);
