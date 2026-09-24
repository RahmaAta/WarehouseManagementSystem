using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Reports.DTOs;

namespace WarehouseManagement.Application.Features.Reports.Queries.GetInventoryValuationReport;

/// <summary>
/// Computes inventory valuation per warehouse.
///
/// Strategy:
///   1. Fetch all matching InventoryItems with Product and Warehouse navigation
///      properties eagerly loaded in a single round-trip.
///   2. Group client-side (the data set is already bounded by the optional
///      WarehouseId filter, so the in-memory grouping is safe and avoids
///      EF Core GroupBy translation limitations with multi-navigation joins).
///   3. Compute per-warehouse: TotalStockValue = SUM(Qty * Price),
///      ReservedStockValue = SUM(ReservedQty * Price).
///   4. AsNoTracking() — read-only report, no EF change tracking needed.
///
/// EF Core GroupBy note:
///   Server-side GroupBy on joined navigation properties often fails to translate
///   when the group key contains navigation-property columns (e.g. Warehouse.Name).
///   Fetching and grouping client-side is the documented EF Core workaround for
///   reports with rich grouping keys.
/// </summary>
public class GetInventoryValuationReportQueryHandler
    : IRequestHandler<GetInventoryValuationReportQuery, IReadOnlyList<InventoryValuationReportDto>>
{
    private readonly IApplicationDbContext _context;

    public GetInventoryValuationReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<InventoryValuationReportDto>> Handle(
        GetInventoryValuationReportQuery request,
        CancellationToken cancellationToken)
    {
        var generatedAt = DateTimeOffset.UtcNow;

        // Build query with eager loading
        var query = _context.InventoryItems
            .AsNoTracking()
            .Include(i => i.Warehouse)
            .Include(i => i.Product)
            .Where(i => i.Product != null && i.Warehouse != null);

        if (request.WarehouseId.HasValue)
            query = query.Where(i => i.WarehouseId == request.WarehouseId.Value);

        if (!request.IncludeInactive)
            query = query.Where(i => i.Warehouse!.IsActive);

        // Materialize (the filter is already applied — safe in-memory grouping)
        var items = await query.ToListAsync(cancellationToken);

        // Group client-side and compute per-warehouse aggregates
        var result = items
            .GroupBy(i => i.WarehouseId)
            .Select(g =>
            {
                var warehouse = g.First().Warehouse!;
                int totalOnHand      = g.Sum(i => i.Quantity);
                int totalReserved    = g.Sum(i => i.ReservedQuantity);
                decimal totalValue   = g.Sum(i => (decimal)i.Quantity        * i.Product!.Price);
                decimal reservedVal  = g.Sum(i => (decimal)i.ReservedQuantity * i.Product!.Price);

                return new InventoryValuationReportDto(
                    WarehouseId:           g.Key,
                    WarehouseName:         warehouse.Name,
                    WarehouseLocation:     warehouse.Location,
                    TotalDistinctProducts: g.Count(),
                    TotalUnitsOnHand:      totalOnHand,
                    TotalUnitsReserved:    totalReserved,
                    TotalUnitsAvailable:   totalOnHand - totalReserved,
                    TotalStockValue:       totalValue,
                    ReservedStockValue:    reservedVal,
                    AvailableStockValue:   totalValue - reservedVal,
                    GeneratedAt:           generatedAt
                );
            })
            .OrderBy(r => r.WarehouseName)
            .ToList()
            .AsReadOnly();

        return result;
    }
}
