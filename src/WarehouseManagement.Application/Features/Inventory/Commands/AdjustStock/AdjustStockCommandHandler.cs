using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Inventory.DTOs;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Features.Inventory.Commands.AdjustStock;


public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, InventoryItemDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AdjustStockCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<InventoryItemDto> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        if (request.ActualCountedQuantity < 0)
        {
            throw new ArgumentException("Actual counted quantity cannot be negative.", nameof(request.ActualCountedQuantity));
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Reason is mandatory for inventory audit stock adjustments.", nameof(request.Reason));
        }

        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == request.WarehouseId, cancellationToken);

        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with ID {request.WarehouseId} was not found.");
        }

        if (!warehouse.IsActive)
        {
            throw new InvalidOperationException($"Cannot adjust stock in deactivated warehouse '{warehouse.Name}'.");
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
            if (request.ActualCountedQuantity == 0)
            {
                throw new InvalidOperationException($"Inventory item does not exist and actual counted quantity is 0.");
            }

            item = new InventoryItem(request.WarehouseId, request.ProductId, request.ActualCountedQuantity);
            _context.InventoryItems.Add(item);

            var initialAdjLog = StockTransaction.CreateAdjustment(
                request.ProductId,
                request.WarehouseId,
                request.ActualCountedQuantity,
                request.ReferenceId,
                request.Reason.Trim(),
                _currentUserService.Username ?? "System");

            _context.StockTransactions.Add(initialAdjLog);
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

        var difference = request.ActualCountedQuantity - item.Quantity;

        if (difference == 0)
        {
            // No discrepancy detected during cycle count
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

        if (difference > 0)
        {
            item.AddStock(difference);
        }
        else
        {
            var unitsToRemove = Math.Abs(difference);
            if (item.AvailableQuantity < unitsToRemove)
            {
                throw new InvalidOperationException(
                    $"Cannot adjust stock to {request.ActualCountedQuantity} because {item.ReservedQuantity} units are currently reserved for pending orders. Available unreserved stock is only {item.AvailableQuantity}.");
            }

            item.RemoveStock(unitsToRemove);
        }

        var adjustmentLog = StockTransaction.CreateAdjustment(
            request.ProductId,
            request.WarehouseId,
            difference,
            request.ReferenceId,
            request.Reason.Trim(),
            _currentUserService.Username ?? "System");

        _context.StockTransactions.Add(adjustmentLog);

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
