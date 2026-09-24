using MediatR;
using WarehouseManagement.Application.Features.PurchaseOrders.DTOs;

namespace WarehouseManagement.Application.Features.PurchaseOrders.Commands.CreatePurchaseOrder;

public record CreatePurchaseOrderItemInput(
    int ProductId,
    int Quantity,
    decimal UnitPrice
);

public record CreatePurchaseOrderCommand(
    int SupplierId,
    int WarehouseId,
    List<CreatePurchaseOrderItemInput> Items,
    string? OrderNumber = null
) : IRequest<PurchaseOrderDto>;
