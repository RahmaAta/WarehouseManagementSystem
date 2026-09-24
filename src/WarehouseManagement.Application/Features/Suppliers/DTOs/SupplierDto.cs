namespace WarehouseManagement.Application.Features.Suppliers.DTOs;

public record SupplierDto(
    int Id,
    string Name,
    string? ContactPerson,
    string Email,
    string PhoneNumber,
    string? Address,
    bool IsActive,
    int TotalPurchaseOrdersCount,
    DateTime CreatedAt
);
