using MediatR;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.SalesOrders.DTOs;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Application.Features.SalesOrders.Queries.GetSalesOrders;

public record GetSalesOrdersQuery(
    int PageNumber = 1,
    int PageSize = 10,
    OrderStatus? Status = null,
    int? CustomerId = null,
    int? WarehouseId = null,
    string? SearchTerm = null
) : IRequest<PaginatedList<SalesOrderDto>>;
