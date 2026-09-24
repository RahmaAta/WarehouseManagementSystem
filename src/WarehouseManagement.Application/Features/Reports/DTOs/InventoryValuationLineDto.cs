namespace WarehouseManagement.Application.Features.Reports.DTOs;

/// <summary>
/// Per-product line item inside the inventory valuation report.
/// Returned alongside the per-warehouse summary for drill-down capability.
/// </summary>
public sealed record InventoryValuationLineDto(
    int ProductId,
    string ProductName,
    string SKU,
    string CategoryName,
    int QuantityOnHand,
    int ReservedQuantity,
    int AvailableQuantity,
    decimal UnitPrice,
    decimal TotalValue
);
