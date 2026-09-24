namespace WarehouseManagement.Application.Common.Interfaces;

/// <summary>
/// Background job that identifies abandoned or stale sales orders (e.g., in Pending
/// status past expiration or unfulfilled for > 48h) and automatically cancels them,
/// safely releasing any reserved stock allocations back to the available inventory pool.
/// </summary>
public interface IStaleOrderCleanupJob
{
    Task<int> ExecuteAsync(CancellationToken cancellationToken = default);
}
