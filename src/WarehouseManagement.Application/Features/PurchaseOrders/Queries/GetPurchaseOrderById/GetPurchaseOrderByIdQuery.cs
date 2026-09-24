using MediatR;
using WarehouseManagement.Application.Features.PurchaseOrders.DTOs;

namespace WarehouseManagement.Application.Features.PurchaseOrders.Queries.GetPurchaseOrderById;

public record GetPurchaseOrderByIdQuery(int Id) : IRequest<PurchaseOrderDto>;
