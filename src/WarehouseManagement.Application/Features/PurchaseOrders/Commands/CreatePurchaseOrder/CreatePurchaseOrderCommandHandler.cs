using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.PurchaseOrders.DTOs;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Features.PurchaseOrders.Commands.CreatePurchaseOrder;

public class CreatePurchaseOrderCommandHandler : IRequestHandler<CreatePurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IApplicationDbContext _context;

    public CreatePurchaseOrderCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PurchaseOrderDto> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Items == null || !request.Items.Any())
        {
            throw new ArgumentException("Purchase order must contain at least one item.", nameof(request.Items));
        }

        // 1. Verify Supplier existence and active status
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken);

        if (supplier == null)
        {
            throw new KeyNotFoundException($"Supplier with ID {request.SupplierId} was not found.");
        }

        if (!supplier.IsActive)
        {
            throw new InvalidOperationException($"Cannot create purchase order for deactivated supplier '{supplier.Name}'.");
        }

        // 2. Verify Warehouse existence and active status
        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == request.WarehouseId, cancellationToken);

        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with ID {request.WarehouseId} was not found.");
        }

        if (!warehouse.IsActive)
        {
            throw new InvalidOperationException($"Cannot create purchase order for deactivated destination warehouse '{warehouse.Name}'.");
        }

        // 3. Verify no duplicate products in order lines
        var duplicateProduct = request.Items
            .GroupBy(i => i.ProductId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateProduct != null)
        {
            throw new InvalidOperationException($"Product ID {duplicateProduct.Key} is duplicated in the purchase order lines.");
        }

        // 4. Verify all products exist and are active
        var productIds = request.Items.Select(i => i.ProductId).ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                throw new KeyNotFoundException($"Product with ID {item.ProductId} was not found.");
            }

            if (!product.IsActive)
            {
                throw new InvalidOperationException($"Cannot include deactivated product '{product.Name}' in purchase order.");
            }

            if (item.Quantity <= 0)
            {
                throw new ArgumentException($"Quantity for product '{product.Name}' must be greater than zero.", nameof(request.Items));
            }

            if (item.UnitPrice < 0)
            {
                throw new ArgumentException($"Unit price for product '{product.Name}' cannot be negative.", nameof(request.Items));
            }
        }

        // 5. Generate or validate OrderNumber
        string orderNumber;
        if (!string.IsNullOrWhiteSpace(request.OrderNumber))
        {
            orderNumber = request.OrderNumber.Trim().ToUpperInvariant();
            var exists = await _context.PurchaseOrders
                .AnyAsync(po => po.OrderNumber == orderNumber, cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException($"Purchase order with number '{orderNumber}' already exists.");
            }
        }
        else
        {
            orderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        }

        // 6. Instantiate Domain Entity with invariants
        var purchaseOrder = new PurchaseOrder(orderNumber, supplier.Id, warehouse.Id);

        foreach (var item in request.Items)
        {
            purchaseOrder.AddItem(item.ProductId, item.Quantity, item.UnitPrice);
        }

        _context.PurchaseOrders.Add(purchaseOrder);
        await _context.SaveChangesAsync(cancellationToken);

        var itemDtos = purchaseOrder.Items.Select(i =>
        {
            var prod = products[i.ProductId];
            return new PurchaseOrderItemDto(
                i.Id,
                i.ProductId,
                prod.Name,
                prod.SKU,
                i.Quantity,
                i.ReceivedQuantity,
                i.UnitPrice,
                i.TotalPrice
            );
        }).ToList();

        return new PurchaseOrderDto(
            purchaseOrder.Id,
            purchaseOrder.OrderNumber,
            supplier.Id,
            supplier.Name,
            warehouse.Id,
            warehouse.Name,
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
