namespace WarehouseManagement.Application.Features.Inventory.DTOs;

public record StockTransactionDto(
    int Id,
    int ProductId,
    string ProductName,
    string ProductSKU,
    int WarehouseId,
    string WarehouseName,
    int? ToWarehouseId,
    string? ToWarehouseName,
    string Type,
    int Quantity,
    string? ReferenceId,
    string? Notes,
    DateTime CreatedAt,
    string? CreatedBy
);
