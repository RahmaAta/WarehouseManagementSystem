namespace WarehouseManagement.Domain.Enums;

/// <summary>
/// Represents the distinct lifecycle states of a customer sales order.
/// </summary>
public enum OrderStatus
{
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3,
    Completed = 4
}
