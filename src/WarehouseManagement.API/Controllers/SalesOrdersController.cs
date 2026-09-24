using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.SalesOrders.Commands.CancelSalesOrder;
using WarehouseManagement.Application.Features.SalesOrders.Commands.CompleteSalesOrder;
using WarehouseManagement.Application.Features.SalesOrders.Commands.ConfirmSalesOrder;
using WarehouseManagement.Application.Features.SalesOrders.Commands.CreateSalesOrder;
using WarehouseManagement.Application.Features.SalesOrders.DTOs;
using WarehouseManagement.Application.Features.SalesOrders.Queries.GetSalesOrderById;
using WarehouseManagement.Application.Features.SalesOrders.Queries.GetSalesOrders;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public SalesOrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieve paginated list of sales orders with filtering by status, customer, warehouse, and search term.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<SalesOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] int? customerId = null,
        [FromQuery] int? warehouseId = null,
        [FromQuery] string? searchTerm = null)
    {
        var result = await _mediator.Send(new GetSalesOrdersQuery(
            pageNumber,
            pageSize,
            status,
            customerId,
            warehouseId,
            searchTerm));

        return Ok(result);
    }

    /// <summary>
    /// Retrieve sales order details by unique ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SalesOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetSalesOrderByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Create a new sales order in Pending status.
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(typeof(SalesOrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateSalesOrderCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Confirm a Pending sales order, atomically validating and reserving required inventory to prevent overselling.
    /// Requires Admin, WarehouseManager, or WarehouseStaff role.
    /// </summary>
    [HttpPost("{id:int}/confirm")]
    [Authorize(Roles = "Admin,WarehouseManager,WarehouseStaff")]
    [ProducesResponseType(typeof(SalesOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Confirm(int id)
    {
        var result = await _mediator.Send(new ConfirmSalesOrderCommand(id));
        return Ok(result);
    }

    /// <summary>
    /// Complete and dispatch a Confirmed sales order, atomically deducting physical stock and writing StockOut audit records.
    /// Requires Admin, WarehouseManager, or WarehouseStaff role.
    /// </summary>
    [HttpPost("{id:int}/complete")]
    [Authorize(Roles = "Admin,WarehouseManager,WarehouseStaff")]
    [ProducesResponseType(typeof(SalesOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Complete(int id)
    {
        var result = await _mediator.Send(new CompleteSalesOrderCommand(id));
        return Ok(result);
    }

    /// <summary>
    /// Cancel a sales order, automatically releasing any reserved inventory back to available stock if the order was Confirmed.
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(typeof(SalesOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _mediator.Send(new CancelSalesOrderCommand(id));
        return Ok(result);
    }
}
