using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.PurchaseOrders.DTOs;

namespace WarehouseManagement.Application.Features.PurchaseOrders.Queries.GetPurchaseOrders;

public class GetPurchaseOrdersQueryHandler : IRequestHandler<GetPurchaseOrdersQuery, PaginatedList<PurchaseOrderDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPurchaseOrdersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<PurchaseOrderDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 10 : (request.PageSize > 100 ? 100 : request.PageSize);

        var query = _context.PurchaseOrders
            .AsNoTracking();

        if (request.Status.HasValue)
        {
            query = query.Where(po => po.Status == request.Status.Value);
        }

        if (request.SupplierId.HasValue)
        {
            query = query.Where(po => po.SupplierId == request.SupplierId.Value);
        }

        if (request.WarehouseId.HasValue)
        {
            query = query.Where(po => po.WarehouseId == request.WarehouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(po => po.OrderNumber.ToLower().Contains(term)
                || (po.Supplier != null && po.Supplier.Name.ToLower().Contains(term)));
        }

        var projected = query
            .OrderByDescending(po => po.CreatedAt)
            .Select(po => new PurchaseOrderDto(
                po.Id,
                po.OrderNumber,
                po.SupplierId,
                po.Supplier != null ? po.Supplier.Name : string.Empty,
                po.WarehouseId,
                po.Warehouse != null ? po.Warehouse.Name : string.Empty,
                po.Status.ToString(),
                po.TotalAmount,
                po.ApprovedBy,
                po.ApprovedAt,
                po.ReceivedBy,
                po.ReceivedAt,
                po.CreatedAt,
                po.Items.Select(i => new PurchaseOrderItemDto(
                    i.Id,
                    i.ProductId,
                    i.Product != null ? i.Product.Name : string.Empty,
                    i.Product != null ? i.Product.SKU : string.Empty,
                    i.Quantity,
                    i.ReceivedQuantity,
                    i.UnitPrice,
                    i.TotalPrice
                )).ToList()
            ));

        return await PaginatedList<PurchaseOrderDto>.CreateAsync(projected, pageNumber, pageSize, cancellationToken);
    }
}
