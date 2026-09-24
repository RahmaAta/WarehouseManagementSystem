namespace WarehouseManagement.Application.Features.SalesOrders.DTOs;

public record SalesOrderItemDto(
    int Id,
    int ProductId,
    string ProductName,
    string ProductSKU,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);
