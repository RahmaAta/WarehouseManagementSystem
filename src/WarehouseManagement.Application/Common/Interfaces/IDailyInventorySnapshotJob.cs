namespace WarehouseManagement.Application.Common.Interfaces;

/// <summary>
/// Background job that runs on a recurring daily schedule to aggregate warehouse
/// metrics (total SKU count, on-hand units, reserved allocations, and total inventory value),
/// ensuring immutable audit continuity and feeding reporting dashboards.
/// </summary>
public interface IDailyInventorySnapshotJob
{
    Task<int> ExecuteAsync(CancellationToken cancellationToken = default);
}
