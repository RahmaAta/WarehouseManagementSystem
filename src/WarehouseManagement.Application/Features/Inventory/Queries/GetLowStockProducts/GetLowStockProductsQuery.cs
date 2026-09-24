using MediatR;

namespace WarehouseManagement.Application.Features.Inventory.Queries.GetLowStockProducts;

public record LowStockProductDto(
    int ProductId,
    string ProductName,
    string ProductSKU,
    int MinimumStockLevel,
    int TotalAvailableStock,
    int DeficitQuantity
);

public record GetLowStockProductsQuery(int? WarehouseId = null) : IRequest<List<LowStockProductDto>>;
