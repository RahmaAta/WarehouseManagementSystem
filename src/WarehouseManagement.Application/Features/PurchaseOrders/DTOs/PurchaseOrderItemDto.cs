namespace WarehouseManagement.Application.Features.PurchaseOrders.DTOs;

public record PurchaseOrderItemDto(
    int Id,
    int ProductId,
    string ProductName,
    string ProductSKU,
    int Quantity,
    int ReceivedQuantity,
    decimal UnitPrice,
    decimal TotalPrice
);
