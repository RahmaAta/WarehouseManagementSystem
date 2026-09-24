using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.SalesOrders.DTOs;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Application.Features.SalesOrders.Commands.CancelSalesOrder;

public class CancelSalesOrderCommandHandler : IRequestHandler<CancelSalesOrderCommand, SalesOrderDto>
{
    private readonly IApplicationDbContext _context;

    public CancelSalesOrderCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SalesOrderDto> Handle(CancelSalesOrderCommand request, CancellationToken cancellationToken)
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

        // Atomicity: Begin explicit database transaction if reservations need to be released
        using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        // If the order was Confirmed, active inventory allocations must be released back to available pool
        if (salesOrder.Status == OrderStatus.Confirmed)
        {
            foreach (var orderItem in salesOrder.Items)
            {
                var inventoryItem = await _context.InventoryItems
                    .FirstOrDefaultAsync(i => i.WarehouseId == salesOrder.WarehouseId && i.ProductId == orderItem.ProductId, cancellationToken);

                if (inventoryItem != null)
                {
                    inventoryItem.ReleaseStock(orderItem.Quantity);
                }
            }
        }

        // Domain method validates that order is not Completed, and transitions to Cancelled
        salesOrder.Cancel();

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
