namespace WarehouseManagement.Domain.Enums;

/// <summary>
/// Represents the distinct lifecycle states of a supplier purchase order.
/// </summary>
public enum PurchaseOrderStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Received = 4,
    Cancelled = 5
}
