namespace WarehouseManagement.Application.Features.PurchaseOrders.DTOs;

public record PurchaseOrderDto(
    int Id,
    string OrderNumber,
    int SupplierId,
    string SupplierName,
    int WarehouseId,
    string WarehouseName,
    string Status,
    decimal TotalAmount,
    string? ApprovedBy,
    DateTime? ApprovedAt,
    string? ReceivedBy,
    DateTime? ReceivedAt,
    DateTime CreatedAt,
    List<PurchaseOrderItemDto> Items
);
