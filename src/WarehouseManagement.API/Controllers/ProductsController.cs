using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.Products.Commands.ActivateProduct;
using WarehouseManagement.Application.Features.Products.Commands.CreateProduct;
using WarehouseManagement.Application.Features.Products.Commands.DeleteProduct;
using WarehouseManagement.Application.Features.Products.Commands.UpdateProduct;
using WarehouseManagement.Application.Features.Products.DTOs;
using WarehouseManagement.Application.Features.Products.Queries.GetProductById;
using WarehouseManagement.Application.Features.Products.Queries.GetProductBySku;
using WarehouseManagement.Application.Features.Products.Queries.GetProducts;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieve paginated products with optional filtering by category, search term (Name/SKU), and active status.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? onlyActive = true)
    {
        var query = new GetProductsQuery(pageNumber, pageSize, categoryId, searchTerm, onlyActive);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve product by its unique internal ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Retrieve product by SKU (Stock Keeping Unit). Ideal for warehouse handheld barcode scanners.
    /// </summary>
    [HttpGet("sku/{sku}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySku(string sku)
    {
        var result = await _mediator.Send(new GetProductBySkuQuery(sku));
        return Ok(result);
    }

    /// <summary>
    /// Register a new product in the catalog.
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update product information and thresholds.
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
    {
        var command = new UpdateProductCommand(
            id,
            request.Name,
            request.SKU,
            request.Price,
            request.MinimumStockLevel,
            request.CategoryId,
            request.Description);

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Soft delete (deactivate) a product.
    /// Requires that product has 0 on-hand units in inventory.
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
        await _mediator.Send(new DeleteProductCommand(id));
        return NoContent();
    }

    /// <summary>
    /// Reactivate a previously deactivated product.
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
        await _mediator.Send(new ActivateProductCommand(id));
        return Ok(new { message = $"Product {id} reactivated successfully." });
    }
}

public record UpdateProductRequest(
    string Name,
    string SKU,
    decimal Price,
    int MinimumStockLevel,
    int CategoryId,
    string? Description
);
