namespace WarehouseManagement.Application.Features.Inventory.DTOs;

public record InventoryItemDto(
    int Id,
    int WarehouseId,
    string WarehouseName,
    int ProductId,
    string ProductName,
    string ProductSKU,
    int Quantity,
    int ReservedQuantity,
    int AvailableQuantity,
    byte[] RowVersion
);
