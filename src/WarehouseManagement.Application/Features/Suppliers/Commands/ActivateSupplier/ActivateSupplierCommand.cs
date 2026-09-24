using MediatR;

namespace WarehouseManagement.Application.Features.Suppliers.Commands.ActivateSupplier;

public record ActivateSupplierCommand(int Id) : IRequest<bool>;
