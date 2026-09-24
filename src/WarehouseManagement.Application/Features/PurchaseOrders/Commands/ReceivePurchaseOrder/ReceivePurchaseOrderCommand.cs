using MediatR;
using WarehouseManagement.Application.Features.PurchaseOrders.DTOs;

namespace WarehouseManagement.Application.Features.PurchaseOrders.Commands.ReceivePurchaseOrder;

public record ReceivePurchaseOrderCommand(
    int Id,
    string? Notes = null
) : IRequest<PurchaseOrderDto>;
