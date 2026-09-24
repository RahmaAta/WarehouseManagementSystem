using MediatR;
using WarehouseManagement.Application.Features.PurchaseOrders.DTOs;

namespace WarehouseManagement.Application.Features.PurchaseOrders.Commands.SubmitPurchaseOrder;

public record SubmitPurchaseOrderCommand(int Id) : IRequest<PurchaseOrderDto>;
