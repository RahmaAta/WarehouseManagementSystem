using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.PurchaseOrders.DTOs;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Domain.Enums;
using WarehouseManagement.Domain.Exceptions;

namespace WarehouseManagement.Application.Features.PurchaseOrders.Commands.ReceivePurchaseOrder;

public class ReceivePurchaseOrderCommandHandler : IRequestHandler<ReceivePurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ReceivePurchaseOrderCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PurchaseOrderDto> Handle(ReceivePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Warehouse)
            .Include(po => po.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(po => po.Id == request.Id, cancellationToken);

        if (purchaseOrder == null)
        {
            throw new KeyNotFoundException($"Purchase order with ID {request.Id} was not found.");
        }

        // Business Rule: Purchase order MUST be in Approved status before goods can be physically received
        if (purchaseOrder.Status != PurchaseOrderStatus.Approved)
        {
            throw new InvalidOrderStateException(nameof(PurchaseOrder), purchaseOrder.Status.ToString(), PurchaseOrderStatus.Received.ToString());
        }

        if (purchaseOrder.Warehouse == null || !purchaseOrder.Warehouse.IsActive)
        {
            throw new InvalidOperationException($"Cannot receive goods into deactivated destination warehouse '{purchaseOrder.Warehouse?.Name}'.");
        }

        var operatorName = _currentUserService.Username ?? "WarehouseStaff";

        // Atomicity: Begin explicit database transaction for inventory mutation + audit ledger
        using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        foreach (var orderItem in purchaseOrder.Items)
        {
            // 1. Locate or create inventory record in target warehouse
            var inventoryItem = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.WarehouseId == purchaseOrder.WarehouseId && i.ProductId == orderItem.ProductId, cancellationToken);

            if (inventoryItem == null)
            {
                inventoryItem = new InventoryItem(purchaseOrder.WarehouseId, orderItem.ProductId, orderItem.Quantity);
                _context.InventoryItems.Add(inventoryItem);
            }
            else
            {
                inventoryItem.AddStock(orderItem.Quantity);
            }

            // 2. Update received quantity on the purchase order line
            orderItem.RecordReceivedQuantity(orderItem.Quantity);

            // 3. Write immutable audit ledger transaction
            var stockInLog = StockTransaction.CreateStockIn(
                orderItem.ProductId,
                purchaseOrder.WarehouseId,
                orderItem.Quantity,
                referenceId: purchaseOrder.OrderNumber,
                notes: request.Notes ?? $"Received PO {purchaseOrder.OrderNumber} from Supplier {purchaseOrder.Supplier?.Name}",
                createdBy: operatorName);

            _context.StockTransactions.Add(stockInLog);
        }

        // 4. Mark purchase order entity as received
        purchaseOrder.MarkAsReceived(operatorName);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var itemDtos = purchaseOrder.Items.Select(i => new PurchaseOrderItemDto(
            i.Id,
            i.ProductId,
            i.Product?.Name ?? string.Empty,
            i.Product?.SKU ?? string.Empty,
            i.Quantity,
            i.ReceivedQuantity,
            i.UnitPrice,
            i.TotalPrice
        )).ToList();

        return new PurchaseOrderDto(
            purchaseOrder.Id,
            purchaseOrder.OrderNumber,
            purchaseOrder.SupplierId,
            purchaseOrder.Supplier?.Name ?? string.Empty,
            purchaseOrder.WarehouseId,
            purchaseOrder.Warehouse.Name,
            purchaseOrder.Status.ToString(),
            purchaseOrder.TotalAmount,
            purchaseOrder.ApprovedBy,
            purchaseOrder.ApprovedAt,
            purchaseOrder.ReceivedBy,
            purchaseOrder.ReceivedAt,
            purchaseOrder.CreatedAt,
            itemDtos
        );
    }
}
