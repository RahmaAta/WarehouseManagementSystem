using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job that scans for abandoned pending or unfulfilled confirmed orders
/// exceeding retention thresholds and cancels them, releasing held stock reservations.
/// </summary>
public class StaleOrderCleanupJob : IStaleOrderCleanupJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<StaleOrderCleanupJob> _logger;

    public StaleOrderCleanupJob(IApplicationDbContext context, ILogger<StaleOrderCleanupJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting StaleOrderCleanupJob execution...");

        var pendingCutoff = DateTime.UtcNow.AddHours(-24);
        var confirmedCutoff = DateTime.UtcNow.AddHours(-48);

        // Find stale pending orders (> 24h) or stale confirmed orders (> 48h without completion)
        var staleOrders = await _context.SalesOrders
            .Include(so => so.Items)
            .Where(so => (so.Status == OrderStatus.Pending && so.CreatedAt <= pendingCutoff) ||
                         (so.Status == OrderStatus.Confirmed && so.CreatedAt <= confirmedCutoff))
            .ToListAsync(cancellationToken);

        if (!staleOrders.Any())
        {
            _logger.LogInformation("No stale sales orders found for cleanup.");
            return 0;
        }

        _logger.LogWarning("Found {Count} stale sales orders to cancel and clean up.", staleOrders.Count);

        using var tx = await _context.BeginTransactionAsync(cancellationToken);

        foreach (var order in staleOrders)
        {
            // If the order had already reserved stock, release the units back to the available pool
            if (order.Status == OrderStatus.Confirmed)
            {
                foreach (var lineItem in order.Items)
                {
                    var inventoryItem = await _context.InventoryItems
                        .FirstOrDefaultAsync(i => i.WarehouseId == order.WarehouseId && i.ProductId == lineItem.ProductId, cancellationToken);

                    if (inventoryItem != null)
                    {
                        inventoryItem.ReleaseStock(lineItem.Quantity);
                        _logger.LogInformation(
                            "Released {Quantity} reserved units of product {ProductId} back to warehouse {WarehouseId} for stale order {OrderNumber}.",
                            lineItem.Quantity, lineItem.ProductId, order.WarehouseId, order.OrderNumber);
                    }
                }
            }

            order.Cancel();
            _logger.LogInformation("Cancelled stale sales order '{OrderNumber}' (Id: {OrderId}).", order.OrderNumber, order.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        _logger.LogInformation("StaleOrderCleanupJob completed. Successfully processed and cancelled {Count} stale orders.", staleOrders.Count);

        return staleOrders.Count;
    }
}
