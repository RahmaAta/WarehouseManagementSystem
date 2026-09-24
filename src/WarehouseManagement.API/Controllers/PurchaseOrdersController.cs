using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.PurchaseOrders.Commands.ApprovePurchaseOrder;
using WarehouseManagement.Application.Features.PurchaseOrders.Commands.CancelPurchaseOrder;
using WarehouseManagement.Application.Features.PurchaseOrders.Commands.CreatePurchaseOrder;
using WarehouseManagement.Application.Features.PurchaseOrders.Commands.ReceivePurchaseOrder;
using WarehouseManagement.Application.Features.PurchaseOrders.Commands.SubmitPurchaseOrder;
using WarehouseManagement.Application.Features.PurchaseOrders.DTOs;
using WarehouseManagement.Application.Features.PurchaseOrders.Queries.GetPurchaseOrderById;
using WarehouseManagement.Application.Features.PurchaseOrders.Queries.GetPurchaseOrders;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public PurchaseOrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieve paginated list of purchase orders with optional filtering by status, supplier, warehouse, and search term.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<PurchaseOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] PurchaseOrderStatus? status = null,
        [FromQuery] int? supplierId = null,
        [FromQuery] int? warehouseId = null,
        [FromQuery] string? searchTerm = null)
    {
        var result = await _mediator.Send(new GetPurchaseOrdersQuery(
            pageNumber,
            pageSize,
            status,
            supplierId,
            warehouseId,
            searchTerm));

        return Ok(result);
    }

    /// <summary>
    /// Retrieve detailed purchase order information by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PurchaseOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetPurchaseOrderByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Create a new purchase order in Draft status.
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(typeof(PurchaseOrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Submit a Draft purchase order for managerial approval.
    /// Requires Admin, WarehouseManager, or WarehouseStaff role.
    /// </summary>
    [HttpPost("{id:int}/submit")]
    [Authorize(Roles = "Admin,WarehouseManager,WarehouseStaff")]
    [ProducesResponseType(typeof(PurchaseOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Submit(int id)
    {
        var result = await _mediator.Send(new SubmitPurchaseOrderCommand(id));
        return Ok(result);
    }

    /// <summary>
    /// Approve a purchase order that is in PendingApproval status.
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(typeof(PurchaseOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await _mediator.Send(new ApprovePurchaseOrderCommand(id));
        return Ok(result);
    }

    /// <summary>
    /// Receive goods against an Approved purchase order, atomically incrementing warehouse inventory and logging stock transactions.
    /// Requires Admin, WarehouseManager, or WarehouseStaff role.
    /// </summary>
    [HttpPost("{id:int}/receive")]
    [Authorize(Roles = "Admin,WarehouseManager,WarehouseStaff")]
    [ProducesResponseType(typeof(PurchaseOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Receive(int id, [FromBody] ReceivePurchaseOrderRequest? request = null)
    {
        var result = await _mediator.Send(new ReceivePurchaseOrderCommand(id, request?.Notes));
        return Ok(result);
    }

    /// <summary>
    /// Cancel a purchase order (allowed only if not yet Received).
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(typeof(PurchaseOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _mediator.Send(new CancelPurchaseOrderCommand(id));
        return Ok(result);
    }
}

public record ReceivePurchaseOrderRequest(string? Notes = null);
