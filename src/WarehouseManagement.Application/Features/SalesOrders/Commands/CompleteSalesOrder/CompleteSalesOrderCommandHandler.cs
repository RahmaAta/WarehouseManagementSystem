using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.SalesOrders.DTOs;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Domain.Enums;
using WarehouseManagement.Domain.Exceptions;

namespace WarehouseManagement.Application.Features.SalesOrders.Commands.CompleteSalesOrder;

public class CompleteSalesOrderCommandHandler : IRequestHandler<CompleteSalesOrderCommand, SalesOrderDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CompleteSalesOrderCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<SalesOrderDto> Handle(CompleteSalesOrderCommand request, CancellationToken cancellationToken)
    {
        var salesOrder = await _context.SalesOrders
            .Include(so => so.Customer)
            .Include(so => so.Warehouse)
            .Include(so => so.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(so => so.Id == request.Id, cancellationToken);

        if (salesOrder == null)
        {
            throw new KeyNotFoundException($"Sales order with ID {request.Id} was not found.");
        }

        // Business Rule: Sales order MUST be in Confirmed status to be fulfilled/completed
        if (salesOrder.Status != OrderStatus.Confirmed)
        {
            throw new InvalidOrderStateException(nameof(SalesOrder), salesOrder.Status.ToString(), OrderStatus.Completed.ToString());
        }

        var operatorName = _currentUserService.Username ?? "WarehouseStaff";

        // Atomicity: Begin explicit database transaction for stock fulfillment + StockOut audit ledger
        using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        foreach (var orderItem in salesOrder.Items)
        {
            var inventoryItem = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.WarehouseId == salesOrder.WarehouseId && i.ProductId == orderItem.ProductId, cancellationToken);

            if (inventoryItem == null)
            {
                throw new InvalidOperationException(
                    $"Inventory item for product ID {orderItem.ProductId} at warehouse ID {salesOrder.WarehouseId} was not found during fulfillment.");
            }

            // Deduct allocated stock from reserved and physical total
            inventoryItem.FulfillReservedStock(orderItem.Quantity);

            // Record immutable StockOut audit trail in transaction ledger
            var stockOutLog = StockTransaction.CreateStockOut(
                orderItem.ProductId,
                salesOrder.WarehouseId,
                orderItem.Quantity,
                referenceId: salesOrder.OrderNumber,
                notes: $"Sales Order {salesOrder.OrderNumber} fulfilled and dispatched to Customer {salesOrder.Customer?.Name}",
                createdBy: operatorName);

            _context.StockTransactions.Add(stockOutLog);
        }

        // Domain method sets Status to Completed
        salesOrder.Complete(operatorName);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var itemDtos = salesOrder.Items.Select(i => new SalesOrderItemDto(
            i.Id,
            i.ProductId,
            i.Product?.Name ?? string.Empty,
            i.Product?.SKU ?? string.Empty,
            i.Quantity,
            i.UnitPrice,
            i.TotalPrice
        )).ToList();

        return new SalesOrderDto(
            salesOrder.Id,
            salesOrder.OrderNumber,
            salesOrder.CustomerId,
            salesOrder.Customer?.Name ?? string.Empty,
            salesOrder.WarehouseId,
            salesOrder.Warehouse?.Name ?? string.Empty,
            salesOrder.Status.ToString(),
            salesOrder.TotalAmount,
            salesOrder.ConfirmedBy,
            salesOrder.ConfirmedAt,
            salesOrder.CompletedBy,
            salesOrder.CompletedAt,
            salesOrder.CreatedAt,
            itemDtos
        );
    }
}
