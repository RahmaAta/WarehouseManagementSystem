using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Inventory.DTOs;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Features.Inventory.Commands.RemoveStock;

public record RemoveStockCommand(
    int WarehouseId,
    int ProductId,
    int Quantity,
    string? ReferenceId = null,
    string? Notes = null
) : IRequest<InventoryItemDto>;

public class RemoveStockCommandHandler : IRequestHandler<RemoveStockCommand, InventoryItemDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RemoveStockCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<InventoryItemDto> Handle(RemoveStockCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            throw new ArgumentException("Quantity to remove must be greater than zero.", nameof(request.Quantity));
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
            throw new InvalidOperationException(
                $"No inventory record exists for product '{product.Name}' in warehouse '{warehouse.Name}'. Available stock is 0.");
        }

        // Domain method validates available stock and throws InsufficientStockException if unavailable
        item.RemoveStock(request.Quantity);

        // Record immutable stock transaction audit log
        var transaction = StockTransaction.CreateStockOut(
            request.ProductId,
            request.WarehouseId,
            request.Quantity,
            request.ReferenceId,
            request.Notes,
            _currentUserService.Username ?? "System");

        _context.StockTransactions.Add(transaction);

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
