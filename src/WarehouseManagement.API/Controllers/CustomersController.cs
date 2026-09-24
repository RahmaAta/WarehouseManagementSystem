using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.Customers.Commands.ActivateCustomer;
using WarehouseManagement.Application.Features.Customers.Commands.CreateCustomer;
using WarehouseManagement.Application.Features.Customers.Commands.DeleteCustomer;
using WarehouseManagement.Application.Features.Customers.Commands.UpdateCustomer;
using WarehouseManagement.Application.Features.Customers.DTOs;
using WarehouseManagement.Application.Features.Customers.Queries.GetCustomerById;
using WarehouseManagement.Application.Features.Customers.Queries.GetCustomers;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieve paginated list of customers with search support.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool includeInactive = false)
    {
        var result = await _mediator.Send(new GetCustomersQuery(pageNumber, pageSize, searchTerm, includeInactive));
        return Ok(result);
    }

    /// <summary>
    /// Retrieve customer details by unique ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetCustomerByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Register a new customer client.
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update customer contact details and address.
    /// Requires Admin or WarehouseManager role.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerRequest request)
    {
        var command = new UpdateCustomerCommand(
            id,
            request.Name,
            request.Email,
            request.PhoneNumber,
            request.Address);

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Soft delete / deactivate a customer.
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
        await _mediator.Send(new DeleteCustomerCommand(id));
        return NoContent();
    }

    /// <summary>
    /// Reactivate a previously deactivated customer.
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
        await _mediator.Send(new ActivateCustomerCommand(id));
        return Ok(new { message = $"Customer {id} reactivated successfully." });
    }
}

public record UpdateCustomerRequest(
    string Name,
    string Email,
    string PhoneNumber,
    string? Address = null
);
