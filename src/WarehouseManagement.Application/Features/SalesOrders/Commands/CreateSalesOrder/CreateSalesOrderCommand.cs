using MediatR;
using WarehouseManagement.Application.Features.SalesOrders.DTOs;

namespace WarehouseManagement.Application.Features.SalesOrders.Commands.CreateSalesOrder;

public record CreateSalesOrderItemInput(
    int ProductId,
    int Quantity,
    decimal UnitPrice
);

public record CreateSalesOrderCommand(
    int CustomerId,
    int WarehouseId,
    List<CreateSalesOrderItemInput> Items,
    string? OrderNumber = null
) : IRequest<SalesOrderDto>;
