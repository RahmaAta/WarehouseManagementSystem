using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Inventory.DTOs;

namespace WarehouseManagement.Application.Features.Inventory.Commands.ReleaseStock;

public record ReleaseStockCommand(
    int WarehouseId,
    int ProductId,
    int Quantity
) : IRequest<InventoryItemDto>;

public class ReleaseStockCommandHandler : IRequestHandler<ReleaseStockCommand, InventoryItemDto>
{
    private readonly IApplicationDbContext _context;

    public ReleaseStockCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryItemDto> Handle(ReleaseStockCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            throw new ArgumentException("Quantity to release must be greater than zero.", nameof(request.Quantity));
        }

        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == request.WarehouseId, cancellationToken);

        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with ID {request.WarehouseId} was not found.");
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {request.ProductId} was not found.");
        }

        var item = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.WarehouseId == request.WarehouseId && i.ProductId == request.ProductId, cancellationToken);

        if (item == null)
        {
            throw new InvalidOperationException($"No inventory record exists for product '{product.Name}' in warehouse '{warehouse.Name}'.");
        }

        // Domain method validates that reserved quantity is sufficient, otherwise throws InvalidOperationException
        item.ReleaseStock(request.Quantity);

        await _context.SaveChangesAsync(cancellationToken);

        return new InventoryItemDto(
            item.Id,
            warehouse.Id,
            warehouse.Name,
            product.Id,
            product.Name,
            product.SKU,
            item.Quantity,
            item.ReservedQuantity,
            item.AvailableQuantity,
            item.RowVersion
        );
    }
}
