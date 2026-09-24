namespace WarehouseManagement.Application.Features.Reports.DTOs;

/// <summary>
/// Aggregated inventory valuation summary per warehouse.
/// Value = SUM(InventoryItem.Quantity * Product.Price) for each warehouse.
/// </summary>
public sealed record InventoryValuationReportDto(
    int WarehouseId,
    string WarehouseName,
    string WarehouseLocation,
    int TotalDistinctProducts,
    int TotalUnitsOnHand,
    int TotalUnitsReserved,
    int TotalUnitsAvailable,
    decimal TotalStockValue,
    decimal ReservedStockValue,
    decimal AvailableStockValue,
    DateTimeOffset GeneratedAt
);
