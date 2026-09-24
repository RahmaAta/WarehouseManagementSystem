namespace WarehouseManagement.Application.Features.Warehouses.DTOs;

public record WarehouseDto(
    int Id,
    string Name,
    string Location,
    bool IsActive,
    int TotalDistinctProducts,
    int TotalStockUnits,
    DateTime CreatedAt
);
