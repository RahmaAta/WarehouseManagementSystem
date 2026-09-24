using MediatR;
using WarehouseManagement.Application.Features.Customers.DTOs;

namespace WarehouseManagement.Application.Features.Customers.Commands.UpdateCustomer;

public record UpdateCustomerCommand(
    int Id,
    string Name,
    string Email,
    string PhoneNumber,
    string? Address = null
) : IRequest<CustomerDto>;
