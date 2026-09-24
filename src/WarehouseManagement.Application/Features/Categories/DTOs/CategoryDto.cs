namespace WarehouseManagement.Application.Features.Categories.DTOs;

public record CategoryDto(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    int ProductsCount
);
