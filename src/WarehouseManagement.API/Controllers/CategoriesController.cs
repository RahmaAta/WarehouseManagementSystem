using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Features.Categories.Commands.CreateCategory;
using WarehouseManagement.Application.Features.Categories.Commands.DeleteCategory;
using WarehouseManagement.Application.Features.Categories.Commands.UpdateCategory;
using WarehouseManagement.Application.Features.Categories.DTOs;
using WarehouseManagement.Application.Features.Categories.Queries.GetCategories;
using WarehouseManagement.Application.Features.Categories.Queries.GetCategoryById;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieve all categories with product counts.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(includeInactive));
        return Ok(result);
    }

    /// <summary>
    /// Retrieve a single category by its unique ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetCategoryByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Create a new product category.
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update an existing product category.
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request)
    {
        var command = new UpdateCategoryCommand(id, request.Name, request.Description);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Delete a category if no products depend on it.
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
        await _mediator.Send(new DeleteCategoryCommand(id));
        return NoContent();
    }
}

public record UpdateCategoryRequest(string Name, string? Description);
