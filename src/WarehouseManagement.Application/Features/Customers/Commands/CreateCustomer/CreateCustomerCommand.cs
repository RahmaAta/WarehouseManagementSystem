using MediatR;
using WarehouseManagement.Application.Features.Customers.DTOs;

namespace WarehouseManagement.Application.Features.Customers.Commands.CreateCustomer;

public record CreateCustomerCommand(
    string Name,
    string Email,
    string PhoneNumber,
    string? Address = null
) : IRequest<CustomerDto>;
