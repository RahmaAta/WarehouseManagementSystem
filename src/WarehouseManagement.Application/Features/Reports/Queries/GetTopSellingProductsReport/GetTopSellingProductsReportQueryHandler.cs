using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Reports.DTOs;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Application.Features.Reports.Queries.GetTopSellingProductsReport;

/// <summary>
/// Returns products ranked by total quantity sold through Completed Sales Orders.
///
/// Key design choice — "Completed only":
///   We count items from orders in status=Completed because stock was physically
///   dispatched. Confirmed orders have stock reserved but not yet shipped.
///   Counting them would inflate the "sold" metric with uncommitted demand.
///
/// Implementation:
///   1. Materialize SalesOrderItems for completed orders in the date range,
///      with Product and Category navigation properties eager-loaded.
///   2. Group client-side by ProductId; rank by TotalQuantitySold.
///   3. Rank assigned via index projection (index + 1) after the bounded Take.
///   4. CategoryId filter applied before materialisation.
/// </summary>
public class GetTopSellingProductsReportQueryHandler
    : IRequestHandler<GetTopSellingProductsReportQuery, TopSellingProductsReportDto>
{
    private readonly IApplicationDbContext _context;

    public GetTopSellingProductsReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TopSellingProductsReportDto> Handle(
        GetTopSellingProductsReportQuery request,
        CancellationToken cancellationToken)
    {
        var generatedAt = DateTimeOffset.UtcNow;
        var from = request.From ?? generatedAt.AddDays(-30);
        var to   = request.To   ?? generatedAt;
        var topN = Math.Clamp(request.TopN, 1, 100);

        // Fetch relevant completed order items with product info
        var itemsQuery = _context.SalesOrderItems
            .AsNoTracking()
            .Include(item => item.SalesOrder)
            .Include(item => item.Product)
                .ThenInclude(p => p!.Category)
            .Where(item =>
                item.SalesOrder!.Status == OrderStatus.Completed &&
                item.SalesOrder.CreatedAt >= from.UtcDateTime &&
                item.SalesOrder.CreatedAt <= to.UtcDateTime);

        if (request.CategoryId.HasValue)
            itemsQuery = itemsQuery.Where(item => item.Product!.CategoryId == request.CategoryId.Value);

        var items = await itemsQuery.ToListAsync(cancellationToken);

        // Group client-side by product, rank by quantity
        var ranked = items
            .GroupBy(i => i.ProductId)
            .Select(g =>
            {
                var product = g.First().Product!;
                int qty     = g.Sum(i => i.Quantity);
                decimal rev = g.Sum(i => (decimal)i.Quantity * i.UnitPrice);
                int orders  = g.Select(i => i.SalesOrderId).Distinct().Count();

                return new
                {
                    ProductId    = g.Key,
                    ProductName  = product.Name,
                    SKU          = product.SKU,
                    CategoryName = product.Category?.Name ?? string.Empty,
                    TotalQuantitySold = qty,
                    TotalRevenue = rev,
                    OrderCount   = orders
                };
            })
            .OrderByDescending(p => p.TotalQuantitySold)
            .Take(topN)
            .Select((p, idx) => new TopSellingProductLineDto(
                Rank:              idx + 1,
                ProductId:         p.ProductId,
                ProductName:       p.ProductName,
                SKU:               p.SKU,
                CategoryName:      p.CategoryName,
                TotalQuantitySold: p.TotalQuantitySold,
                TotalRevenue:      p.TotalRevenue,
                AverageUnitPrice:  p.TotalQuantitySold > 0
                    ? p.TotalRevenue / p.TotalQuantitySold
                    : 0m,
                OrderCount:        p.OrderCount
            ))
            .ToList()
            .AsReadOnly();

        return new TopSellingProductsReportDto(
            From:        from,
            To:          to,
            TopN:        topN,
            Products:    ranked,
            GeneratedAt: generatedAt
        );
    }
}
