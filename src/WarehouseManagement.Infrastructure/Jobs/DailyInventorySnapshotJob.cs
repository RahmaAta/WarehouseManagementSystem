using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WarehouseManagement.Application.Common.Interfaces;

namespace WarehouseManagement.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job that calculates daily inventory balances and valuation metrics
/// across all active warehouses, generating structured audit summaries.
/// </summary>
public class DailyInventorySnapshotJob : IDailyInventorySnapshotJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<DailyInventorySnapshotJob> _logger;

    public DailyInventorySnapshotJob(IApplicationDbContext context, ILogger<DailyInventorySnapshotJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting DailyInventorySnapshotJob execution...");

        var inventoryStats = await _context.InventoryItems
            .AsNoTracking()
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .Where(i => i.Warehouse != null && i.Warehouse.IsActive && i.Product != null && i.Product.IsActive)
            .Select(i => new
            {
                i.WarehouseId,
                WarehouseName = i.Warehouse!.Name,
                i.ProductId,
                ProductName = i.Product!.Name,
                ProductSKU = i.Product.SKU,
                UnitPrice = i.Product.Price,
                i.Quantity,
                i.ReservedQuantity,
                i.AvailableQuantity,
                ItemValuation = i.Quantity * i.Product.Price
            })
            .ToListAsync(cancellationToken);

        var totalUnitsOnHand = inventoryStats.Sum(s => s.Quantity);
        var totalReservedUnits = inventoryStats.Sum(s => s.ReservedQuantity);
        var totalAvailableUnits = inventoryStats.Sum(s => s.AvailableQuantity);
        var totalInventoryValuation = inventoryStats.Sum(s => s.ItemValuation);
        var distinctProductsCount = inventoryStats.Select(s => s.ProductId).Distinct().Count();
        var distinctWarehousesCount = inventoryStats.Select(s => s.WarehouseId).Distinct().Count();

        _logger.LogInformation(
            "=== DAILY INVENTORY VALUATION & AUDIT SNAPSHOT ===\n" +
            "Date: {SnapshotDate:yyyy-MM-dd HH:mm:ss} UTC\n" +
            "Active Warehouses: {WarehousesCount}\n" +
            "Catalog Products with Stock: {ProductsCount}\n" +
            "Total Physical On-Hand Units: {TotalUnits:N0}\n" +
            "Total Reserved Allocations: {TotalReserved:N0}\n" +
            "Total Available Units: {TotalAvailable:N0}\n" +
            "Total Warehouse Inventory Valuation: ${Valuation:N2}\n" +
            "==================================================",
            DateTime.UtcNow,
            distinctWarehousesCount,
            distinctProductsCount,
            totalUnitsOnHand,
            totalReservedUnits,
            totalAvailableUnits,
            totalInventoryValuation);

        return inventoryStats.Count;
    }
}
