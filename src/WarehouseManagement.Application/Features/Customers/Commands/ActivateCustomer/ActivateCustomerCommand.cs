using MediatR;

namespace WarehouseManagement.Application.Features.Customers.Commands.ActivateCustomer;

public record ActivateCustomerCommand(int Id) : IRequest<bool>;
