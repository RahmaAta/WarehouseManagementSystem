using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WarehouseManagement.Application.Common.Interfaces;

namespace WarehouseManagement.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job that detects products falling below their minimum stock thresholds
/// and logs actionable procurement replenishment alerts.
/// </summary>
public class LowStockNotifierJob : ILowStockNotifierJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<LowStockNotifierJob> _logger;

    public LowStockNotifierJob(IApplicationDbContext context, ILogger<LowStockNotifierJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting LowStockNotifierJob execution...");

        var activeProducts = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.SKU,
                p.MinimumStockLevel,
                TotalQuantity = _context.InventoryItems
                    .Where(i => i.ProductId == p.Id)
                    .Sum(i => (int?)i.Quantity) ?? 0,
                TotalReserved = _context.InventoryItems
                    .Where(i => i.ProductId == p.Id)
                    .Sum(i => (int?)i.ReservedQuantity) ?? 0
            })
            .ToListAsync(cancellationToken);

        var lowStockProducts = activeProducts
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.SKU,
                p.MinimumStockLevel,
                Available = p.TotalQuantity - p.TotalReserved,
                Deficit = p.MinimumStockLevel - (p.TotalQuantity - p.TotalReserved)
            })
            .Where(p => p.Available <= p.MinimumStockLevel)
            .ToList();

        foreach (var item in lowStockProducts)
        {
            _logger.LogWarning(
                "LOW STOCK ALERT: Product '{ProductName}' (SKU: {SKU}) is below safety threshold! Available: {Available}, MinThreshold: {MinThreshold}, Replenishment Deficit: {Deficit}",
                item.Name, item.SKU, item.Available, item.MinimumStockLevel, item.Deficit);
        }

        _logger.LogInformation(
            "LowStockNotifierJob completed. Scanned {TotalProducts} active products; {LowStockCount} items require replenishment.",
            activeProducts.Count, lowStockProducts.Count);

        return lowStockProducts.Count;
    }
}
