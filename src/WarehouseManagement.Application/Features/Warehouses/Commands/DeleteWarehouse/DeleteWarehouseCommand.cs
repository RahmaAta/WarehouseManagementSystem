using MediatR;

namespace WarehouseManagement.Application.Features.Warehouses.Commands.DeleteWarehouse;

public record DeleteWarehouseCommand(int Id) : IRequest<bool>;
