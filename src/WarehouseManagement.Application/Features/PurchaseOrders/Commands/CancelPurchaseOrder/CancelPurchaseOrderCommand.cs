using MediatR;
using WarehouseManagement.Application.Features.PurchaseOrders.DTOs;

namespace WarehouseManagement.Application.Features.PurchaseOrders.Commands.CancelPurchaseOrder;

public record CancelPurchaseOrderCommand(int Id) : IRequest<PurchaseOrderDto>;
