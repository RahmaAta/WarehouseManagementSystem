using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.SalesOrders.DTOs;

namespace WarehouseManagement.Application.Features.SalesOrders.Queries.GetSalesOrders;

public class GetSalesOrdersQueryHandler : IRequestHandler<GetSalesOrdersQuery, PaginatedList<SalesOrderDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSalesOrdersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<SalesOrderDto>> Handle(GetSalesOrdersQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 10 : (request.PageSize > 100 ? 100 : request.PageSize);

        var query = _context.SalesOrders
            .AsNoTracking();

        if (request.Status.HasValue)
        {
            query = query.Where(so => so.Status == request.Status.Value);
        }

        if (request.CustomerId.HasValue)
        {
            query = query.Where(so => so.CustomerId == request.CustomerId.Value);
        }

        if (request.WarehouseId.HasValue)
        {
            query = query.Where(so => so.WarehouseId == request.WarehouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(so => so.OrderNumber.ToLower().Contains(term)
                || (so.Customer != null && so.Customer.Name.ToLower().Contains(term)));
        }

        var projected = query
            .OrderByDescending(so => so.CreatedAt)
            .Select(so => new SalesOrderDto(
                so.Id,
                so.OrderNumber,
                so.CustomerId,
                so.Customer != null ? so.Customer.Name : string.Empty,
                so.WarehouseId,
                so.Warehouse != null ? so.Warehouse.Name : string.Empty,
                so.Status.ToString(),
                so.TotalAmount,
                so.ConfirmedBy,
                so.ConfirmedAt,
                so.CompletedBy,
                so.CompletedAt,
                so.CreatedAt,
                so.Items.Select(i => new SalesOrderItemDto(
                    i.Id,
                    i.ProductId,
                    i.Product != null ? i.Product.Name : string.Empty,
                    i.Product != null ? i.Product.SKU : string.Empty,
                    i.Quantity,
                    i.UnitPrice,
                    i.TotalPrice
                )).ToList()
            ));

        return await PaginatedList<SalesOrderDto>.CreateAsync(projected, pageNumber, pageSize, cancellationToken);
    }
}
