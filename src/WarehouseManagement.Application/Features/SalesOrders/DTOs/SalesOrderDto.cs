namespace WarehouseManagement.Application.Features.SalesOrders.DTOs;

public record SalesOrderDto(
    int Id,
    string OrderNumber,
    int CustomerId,
    string CustomerName,
    int WarehouseId,
    string WarehouseName,
    string Status,
    decimal TotalAmount,
    string? ConfirmedBy,
    DateTime? ConfirmedAt,
    string? CompletedBy,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    List<SalesOrderItemDto> Items
);
