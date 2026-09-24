namespace WarehouseManagement.Domain.Enums;

/// <summary>
/// Categorizes the immutable movement of inventory stock across physical locations.
/// </summary>
public enum StockTransactionType
{
    StockIn = 1,
    StockOut = 2,
    Transfer = 3,
    Adjustment = 4
}
