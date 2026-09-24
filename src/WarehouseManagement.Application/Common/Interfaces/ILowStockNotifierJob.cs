namespace WarehouseManagement.Application.Common.Interfaces;

/// <summary>
/// Background job that scans active products whose aggregated available stock
/// has fallen below the defined minimum safety threshold (MinimumStockLevel),
/// generating alert notifications for procurement and warehouse managers.
/// </summary>
public interface ILowStockNotifierJob
{
    Task<int> ExecuteAsync(CancellationToken cancellationToken = default);
}
