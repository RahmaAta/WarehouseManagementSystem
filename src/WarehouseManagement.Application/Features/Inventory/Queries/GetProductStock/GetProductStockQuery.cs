using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Inventory.DTOs;

namespace WarehouseManagement.Application.Features.Inventory.Queries.GetProductStock;

public record GetProductStockQuery(int ProductId) : IRequest<List<InventoryItemDto>>;

public class GetProductStockQueryHandler : IRequestHandler<GetProductStockQuery, List<InventoryItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProductStockQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<InventoryItemDto>> Handle(GetProductStockQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {request.ProductId} was not found.");
        }

        return await _context.InventoryItems
            .AsNoTracking()
            .Where(i => i.ProductId == request.ProductId)
            .Include(i => i.Warehouse)
            .OrderBy(i => i.Warehouse != null ? i.Warehouse.Name : string.Empty)
            .Select(i => new InventoryItemDto(
                i.Id,
                i.WarehouseId,
                i.Warehouse != null ? i.Warehouse.Name : string.Empty,
                i.ProductId,
                product.Name,
                product.SKU,
                i.Quantity,
                i.ReservedQuantity,
                i.Quantity - i.ReservedQuantity,
                i.RowVersion
            ))
            .ToListAsync(cancellationToken);
    }
}
