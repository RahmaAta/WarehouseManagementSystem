using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Domain.Exceptions;

namespace WarehouseManagement.Application.Features.Inventory.Commands.TransferStock;

public class TransferStockCommandHandler : IRequestHandler<TransferStockCommand, StockTransferResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public TransferStockCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<StockTransferResultDto> Handle(TransferStockCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            throw new ArgumentException("Transfer quantity must be greater than zero.", nameof(request.Quantity));
        }

        if (request.FromWarehouseId == request.ToWarehouseId)
        {
            throw new SameWarehouseTransferException(request.FromWarehouseId);
        }

        var fromWarehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == request.FromWarehouseId, cancellationToken);

        if (fromWarehouse == null)
        {
            throw new KeyNotFoundException($"Source warehouse with ID {request.FromWarehouseId} was not found.");
        }

        if (!fromWarehouse.IsActive)
        {
            throw new InvalidOperationException($"Cannot transfer stock from deactivated warehouse '{fromWarehouse.Name}'.");
        }

        var toWarehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == request.ToWarehouseId, cancellationToken);

        if (toWarehouse == null)
        {
            throw new KeyNotFoundException($"Destination warehouse with ID {request.ToWarehouseId} was not found.");
        }

        if (!toWarehouse.IsActive)
        {
            throw new InvalidOperationException($"Cannot transfer stock to deactivated warehouse '{toWarehouse.Name}'.");
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {request.ProductId} was not found.");
        }

        if (!product.IsActive)
        {
            throw new InvalidOperationException($"Cannot transfer deactivated product '{product.Name}'.");
        }

        // Atomicity: Begin explicit database transaction
        using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        // Deduct from source warehouse
        var sourceItem = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.WarehouseId == request.FromWarehouseId && i.ProductId == request.ProductId, cancellationToken);

        if (sourceItem == null)
        {
            throw new InsufficientStockException(request.ProductId, request.FromWarehouseId, request.Quantity, availableQuantity: 0);
        }

        sourceItem.RemoveStock(request.Quantity);

        // Add to target warehouse
        var targetItem = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.WarehouseId == request.ToWarehouseId && i.ProductId == request.ProductId, cancellationToken);

        if (targetItem == null)
        {
            targetItem = new InventoryItem(request.ToWarehouseId, request.ProductId, request.Quantity);
            _context.InventoryItems.Add(targetItem);
        }
        else
        {
            targetItem.AddStock(request.Quantity);
        }

        // Record immutable transfer audit log
        var stockTransferLog = StockTransaction.CreateTransfer(
            request.ProductId,
            request.FromWarehouseId,
            request.ToWarehouseId,
            request.Quantity,
            request.ReferenceId,
            request.Notes,
            _currentUserService.Username ?? "System");

        _context.StockTransactions.Add(stockTransferLog);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new StockTransferResultDto(
            Success: true,
            ProductId: product.Id,
            ProductName: product.Name,
            ProductSKU: product.SKU,
            FromWarehouseId: fromWarehouse.Id,
            FromWarehouseName: fromWarehouse.Name,
            ToWarehouseId: toWarehouse.Id,
            ToWarehouseName: toWarehouse.Name,
            Quantity: request.Quantity,
            TransferredAt: stockTransferLog.CreatedAt
        );
    }
}
