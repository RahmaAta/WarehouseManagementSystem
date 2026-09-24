using MediatR;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.Inventory.DTOs;

namespace WarehouseManagement.Application.Features.Warehouses.Queries.GetWarehouseStock;

public record GetWarehouseStockQuery(
    int WarehouseId,
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null
) : IRequest<PaginatedList<InventoryItemDto>>;
