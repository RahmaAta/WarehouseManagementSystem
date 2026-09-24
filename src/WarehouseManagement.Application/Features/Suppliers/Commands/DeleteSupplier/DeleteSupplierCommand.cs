using MediatR;

namespace WarehouseManagement.Application.Features.Suppliers.Commands.DeleteSupplier;

public record DeleteSupplierCommand(int Id) : IRequest<bool>;
