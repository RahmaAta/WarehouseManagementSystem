namespace WarehouseManagement.Application.Features.Products.DTOs;

public record ProductDto(
    int Id,
    string Name,
    string SKU,
    string? Description,
    decimal Price,
    int MinimumStockLevel,
    int CategoryId,
    string CategoryName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastModifiedAt,
    int TotalAvailableStock
);
