using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;

namespace WarehouseManagement.Application.Features.Inventory.Queries.GetLowStockProducts;

public class GetLowStockProductsQueryHandler : IRequestHandler<GetLowStockProductsQuery, List<LowStockProductDto>>
{
    private readonly IApplicationDbContext _context;

    public GetLowStockProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LowStockProductDto>> Handle(GetLowStockProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Include(p => p.InventoryItems)
            .ToListAsync(cancellationToken);

        var lowStockList = new List<LowStockProductDto>();

        foreach (var product in products)
        {
            var items = request.WarehouseId.HasValue
                ? product.InventoryItems.Where(i => i.WarehouseId == request.WarehouseId.Value)
                : product.InventoryItems;

            var totalAvailable = items.Sum(i => i.AvailableQuantity);

            if (totalAvailable <= product.MinimumStockLevel)
            {
                lowStockList.Add(new LowStockProductDto(
                    product.Id,
                    product.Name,
                    product.SKU,
                    product.MinimumStockLevel,
                    totalAvailable,
                    DeficitQuantity: Math.Max(0, product.MinimumStockLevel - totalAvailable)
                ));
            }
        }

        return lowStockList.OrderByDescending(x => x.DeficitQuantity).ToList();
    }
}
