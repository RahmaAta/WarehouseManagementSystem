using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Reports.DTOs;

namespace WarehouseManagement.Application.Features.Reports.Queries.GetWarehouseUtilizationReport;

/// <summary>
/// Fetches all active inventory items with product/warehouse details and
/// assembles a rich utilization snapshot.
///
/// Strategy:
///   - Single query with eager-loading (Include) rather than N+1 per warehouse.
///   - Low-stock flag: QuantityOnHand &lt; Product.MinimumStockLevel.
///   - Out-of-stock flag: QuantityOnHand == 0.
///   - StockValue: Quantity * Product.Price (total on-hand valuation).
///   - Client-side grouping avoids EF Core GroupBy translation limitations
///     for navigation-property group keys.
///   - IncludeProductDetails controls whether per-product rows are returned
///     or replaced with an empty array (saves bandwidth for dashboard summaries).
/// </summary>
public class GetWarehouseUtilizationReportQueryHandler
    : IRequestHandler<GetWarehouseUtilizationReportQuery, WarehouseUtilizationReportDto>
{
    private readonly IApplicationDbContext _context;

    public GetWarehouseUtilizationReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WarehouseUtilizationReportDto> Handle(
        GetWarehouseUtilizationReportQuery request,
        CancellationToken cancellationToken)
    {
        var generatedAt = DateTimeOffset.UtcNow;

        // Single query with eager loading
        var query = _context.InventoryItems
            .AsNoTracking()
            .Include(i => i.Warehouse)
            .Include(i => i.Product)
            .Where(i => i.Warehouse != null && i.Warehouse.IsActive && i.Product != null);

        if (request.WarehouseId.HasValue)
            query = query.Where(i => i.WarehouseId == request.WarehouseId.Value);

        var items = await query.ToListAsync(cancellationToken);

        // Group by WarehouseId (int) client-side
        var warehouses = items
            .GroupBy(i => i.WarehouseId)
            .OrderBy(g => g.First().Warehouse!.Name)
            .Select(g =>
            {
                var warehouse = g.First().Warehouse!;

                var productLines = g.Select(i =>
                {
                    bool isLow = i.Quantity < i.Product!.MinimumStockLevel;
                    bool isOut = i.Quantity == 0;
                    return new WarehouseProductStockEntryDto(
                        ProductId:         i.ProductId,
                        ProductName:       i.Product!.Name,
                        SKU:               i.Product.SKU,
                        QuantityOnHand:    i.Quantity,
                        ReservedQuantity:  i.ReservedQuantity,
                        AvailableQuantity: i.Quantity - i.ReservedQuantity,
                        MinimumStockLevel: i.Product.MinimumStockLevel,
                        IsLowStock:        isLow,
                        IsOutOfStock:      isOut,
                        StockValue:        (decimal)i.Quantity * i.Product.Price
                    );
                }).ToList();

                return new WarehouseUtilizationEntryDto(
                    WarehouseId:            g.Key,
                    WarehouseName:          warehouse.Name,
                    WarehouseLocation:      warehouse.Location,
                    TotalDistinctProducts:  productLines.Count,
                    TotalUnitsOnHand:       productLines.Sum(p => p.QuantityOnHand),
                    TotalUnitsReserved:     productLines.Sum(p => p.ReservedQuantity),
                    TotalUnitsAvailable:    productLines.Sum(p => p.AvailableQuantity),
                    LowStockProductCount:   productLines.Count(p => p.IsLowStock),
                    OutOfStockProductCount: productLines.Count(p => p.IsOutOfStock),
                    TotalStockValue:        productLines.Sum(p => p.StockValue),
                    Products: request.IncludeProductDetails
                        ? productLines.AsReadOnly()
                        : (IReadOnlyList<WarehouseProductStockEntryDto>)Array.Empty<WarehouseProductStockEntryDto>()
                );
            })
            .ToList()
            .AsReadOnly();

        return new WarehouseUtilizationReportDto(
            Warehouses:  warehouses,
            GeneratedAt: generatedAt
        );
    }
}
