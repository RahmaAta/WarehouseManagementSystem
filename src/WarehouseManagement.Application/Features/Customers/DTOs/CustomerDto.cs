namespace WarehouseManagement.Application.Features.Customers.DTOs;

public record CustomerDto(
    int Id,
    string Name,
    string Email,
    string PhoneNumber,
    string? Address,
    bool IsActive,
    int TotalSalesOrdersCount,
    DateTime CreatedAt
);
