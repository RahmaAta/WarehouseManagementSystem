using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.Inventory.DTOs;

namespace WarehouseManagement.Application.Features.Warehouses.Queries.GetWarehouseStock;

public record GetWarehouseStockQuery(
    int WarehouseId,
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null
) : IRequest<PaginatedList<InventoryItemDto>>;

public class GetWarehouseStockQueryHandler : IRequestHandler<GetWarehouseStockQuery, PaginatedList<InventoryItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetWarehouseStockQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<InventoryItemDto>> Handle(GetWarehouseStockQuery request, CancellationToken cancellationToken)
    {
        var warehouseExists = await _context.Warehouses
            .AnyAsync(w => w.Id == request.WarehouseId, cancellationToken);

        if (!warehouseExists)
        {
            throw new KeyNotFoundException($"Warehouse with ID {request.WarehouseId} was not found.");
        }

        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 10 : (request.PageSize > 100 ? 100 : request.PageSize);

        var query = _context.InventoryItems
            .AsNoTracking()
            .Where(i => i.WarehouseId == request.WarehouseId);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(i => i.Product != null && (i.Product.Name.Contains(term) || i.Product.SKU.Contains(term)));
        }

        var projected = query
            .OrderBy(i => i.Product != null ? i.Product.Name : string.Empty)
            .Select(i => new InventoryItemDto(
                i.Id,
                i.WarehouseId,
                i.Warehouse != null ? i.Warehouse.Name : string.Empty,
                i.ProductId,
                i.Product != null ? i.Product.Name : string.Empty,
                i.Product != null ? i.Product.SKU : string.Empty,
                i.Quantity,
                i.ReservedQuantity,
                i.Quantity - i.ReservedQuantity,
                i.RowVersion
            ));

        return await PaginatedList<InventoryItemDto>.CreateAsync(projected, pageNumber, pageSize, cancellationToken);
    }
}
