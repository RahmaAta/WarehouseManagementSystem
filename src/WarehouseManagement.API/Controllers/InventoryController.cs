using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.Inventory.Commands.AddStock;
using WarehouseManagement.Application.Features.Inventory.Commands.RemoveStock;
using WarehouseManagement.Application.Features.Inventory.Commands.TransferStock;
using WarehouseManagement.Application.Features.Inventory.DTOs;
using WarehouseManagement.Application.Features.Inventory.Queries.GetProductStock;
using WarehouseManagement.Application.Features.Inventory.Queries.GetStockTransactions;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieve inventory distribution and quantity breakdown across all warehouses for a specific product.
    /// </summary>
    [HttpGet("product/{productId:int}")]
    [ProducesResponseType(typeof(List<InventoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductStock(int productId)
    {
        var result = await _mediator.Send(new GetProductStockQuery(productId));
        return Ok(result);
    }

    /// <summary>
    /// Retrieve paginated immutable stock transaction audit logs (filter by product or warehouse).
    /// </summary>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(PaginatedList<StockTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int? productId = null,
        [FromQuery] int? warehouseId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetStockTransactionsQuery(productId, warehouseId, pageNumber, pageSize));
        return Ok(result);
    }

    /// <summary>
    /// Add stock units to a specific warehouse location and record a StockIn audit log.
    /// Permitted for Admin, WarehouseManager, and WarehouseStaff roles.
    /// </summary>
    [HttpPost("add-stock")]
    [Authorize(Roles = "Admin,WarehouseManager,WarehouseStaff")]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddStock([FromBody] AddStockCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Remove stock units from a warehouse location and record a StockOut audit log.
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpPost("remove-stock")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveStock([FromBody] RemoveStockCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Transfer stock units atomically between two warehouses and record a Transfer audit log.
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpPost("transfer-stock")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(typeof(StockTransferResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> TransferStock([FromBody] TransferStockCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
