using MediatR;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.PurchaseOrders.DTOs;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Application.Features.PurchaseOrders.Queries.GetPurchaseOrders;

public record GetPurchaseOrdersQuery(
    int PageNumber = 1,
    int PageSize = 10,
    PurchaseOrderStatus? Status = null,
    int? SupplierId = null,
    int? WarehouseId = null,
    string? SearchTerm = null
) : IRequest<PaginatedList<PurchaseOrderDto>>;
