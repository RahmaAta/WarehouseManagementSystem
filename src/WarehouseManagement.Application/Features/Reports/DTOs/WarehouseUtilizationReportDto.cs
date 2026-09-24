namespace WarehouseManagement.Application.Features.Reports.DTOs;

/// <summary>
/// Warehouse utilization report: a snapshot of how fully stocked each warehouse is,
/// measured as total units on hand vs a computed capacity ceiling
/// (sum of all product MinimumStockLevel * CapacityMultiplier).
/// </summary>
public sealed record WarehouseUtilizationReportDto(
    IReadOnlyList<WarehouseUtilizationEntryDto> Warehouses,
    DateTimeOffset GeneratedAt
);

/// <summary>One warehouse's utilization metrics.</summary>
public sealed record WarehouseUtilizationEntryDto(
    int WarehouseId,
    string WarehouseName,
    string WarehouseLocation,
    int TotalDistinctProducts,
    int TotalUnitsOnHand,
    int TotalUnitsReserved,
    int TotalUnitsAvailable,
    int LowStockProductCount,
    int OutOfStockProductCount,
    decimal TotalStockValue,
    IReadOnlyList<WarehouseProductStockEntryDto> Products
);

/// <summary>Per-product stock entry inside a warehouse utilization entry.</summary>
public sealed record WarehouseProductStockEntryDto(
    int ProductId,
    string ProductName,
    string SKU,
    int QuantityOnHand,
    int ReservedQuantity,
    int AvailableQuantity,
    int MinimumStockLevel,
    bool IsLowStock,
    bool IsOutOfStock,
    decimal StockValue
);
