using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.SalesOrders.DTOs;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Domain.Enums;
using WarehouseManagement.Domain.Exceptions;

namespace WarehouseManagement.Application.Features.SalesOrders.Commands.ConfirmSalesOrder;

public class ConfirmSalesOrderCommandHandler : IRequestHandler<ConfirmSalesOrderCommand, SalesOrderDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ConfirmSalesOrderCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<SalesOrderDto> Handle(ConfirmSalesOrderCommand request, CancellationToken cancellationToken)
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

        if (salesOrder.Status != OrderStatus.Pending)
        {
            throw new InvalidOrderStateException(nameof(SalesOrder), salesOrder.Status.ToString(), OrderStatus.Confirmed.ToString());
        }

        if (salesOrder.Warehouse == null || !salesOrder.Warehouse.IsActive)
        {
            throw new InvalidOperationException($"Cannot confirm sales order from deactivated warehouse '{salesOrder.Warehouse?.Name}'.");
        }

        var operatorName = _currentUserService.Username ?? "SalesManager";

        // Atomicity: Begin explicit database transaction to reserve inventory
        using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        foreach (var orderItem in salesOrder.Items)
        {
            var inventoryItem = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.WarehouseId == salesOrder.WarehouseId && i.ProductId == orderItem.ProductId, cancellationToken);

            if (inventoryItem == null)
            {
                throw new InsufficientStockException(orderItem.ProductId, salesOrder.WarehouseId, orderItem.Quantity, availableQuantity: 0);
            }

            // Domain method checks AvailableQuantity and increments ReservedQuantity
            inventoryItem.ReserveStock(orderItem.Quantity);
        }

        // Domain method sets Status to Confirmed
        salesOrder.Confirm(operatorName);

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
            salesOrder.Warehouse.Name,
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
