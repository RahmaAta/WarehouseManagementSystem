using MediatR;

namespace WarehouseManagement.Application.Features.Customers.Commands.DeleteCustomer;

public record DeleteCustomerCommand(int Id) : IRequest<bool>;
