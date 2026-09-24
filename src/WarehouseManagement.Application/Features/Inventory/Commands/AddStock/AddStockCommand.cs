using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Inventory.DTOs;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Features.Inventory.Commands.AddStock;

public record AddStockCommand(
    int WarehouseId,
    int ProductId,
    int Quantity,
    string? ReferenceId = null,
    string? Notes = null
) : IRequest<InventoryItemDto>;

public class AddStockCommandHandler : IRequestHandler<AddStockCommand, InventoryItemDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddStockCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<InventoryItemDto> Handle(AddStockCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            throw new ArgumentException("Quantity to add must be greater than zero.", nameof(request.Quantity));
        }

        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == request.WarehouseId, cancellationToken);

        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with ID {request.WarehouseId} was not found.");
        }

        if (!warehouse.IsActive)
        {
            throw new InvalidOperationException($"Cannot add stock to deactivated warehouse '{warehouse.Name}'.");
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {request.ProductId} was not found.");
        }

        if (!product.IsActive)
        {
            throw new InvalidOperationException($"Cannot add stock for deactivated product '{product.Name}'.");
        }

        // Locate existing inventory item or initialize new record for this product at this warehouse
        var item = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.WarehouseId == request.WarehouseId && i.ProductId == request.ProductId, cancellationToken);

        if (item == null)
        {
            item = new InventoryItem(request.WarehouseId, request.ProductId, request.Quantity);
            _context.InventoryItems.Add(item);
        }
        else
        {
            item.AddStock(request.Quantity);
        }

        // Record immutable stock transaction audit log
        var transaction = StockTransaction.CreateStockIn(
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
