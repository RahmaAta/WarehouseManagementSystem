using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.SalesOrders.DTOs;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Features.SalesOrders.Commands.CreateSalesOrder;

public class CreateSalesOrderCommandHandler : IRequestHandler<CreateSalesOrderCommand, SalesOrderDto>
{
    private readonly IApplicationDbContext _context;

    public CreateSalesOrderCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SalesOrderDto> Handle(CreateSalesOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Items == null || !request.Items.Any())
        {
            throw new ArgumentException("Sales order must contain at least one item.", nameof(request.Items));
        }

        // 1. Verify Customer exists and is active
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (customer == null)
        {
            throw new KeyNotFoundException($"Customer with ID {request.CustomerId} was not found.");
        }

        if (!customer.IsActive)
        {
            throw new InvalidOperationException($"Cannot create sales order for deactivated customer '{customer.Name}'.");
        }

        // 2. Verify Warehouse exists and is active
        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == request.WarehouseId, cancellationToken);

        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with ID {request.WarehouseId} was not found.");
        }

        if (!warehouse.IsActive)
        {
            throw new InvalidOperationException($"Cannot create sales order for deactivated warehouse '{warehouse.Name}'.");
        }

        // 3. Verify no duplicate products
        var duplicateProduct = request.Items
            .GroupBy(i => i.ProductId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateProduct != null)
        {
            throw new InvalidOperationException($"Product ID {duplicateProduct.Key} is duplicated in the sales order lines.");
        }

        // 4. Verify products exist and are active
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
                throw new InvalidOperationException($"Cannot include deactivated product '{product.Name}' in sales order.");
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
            var exists = await _context.SalesOrders
                .AnyAsync(so => so.OrderNumber == orderNumber, cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException($"Sales order with number '{orderNumber}' already exists.");
            }
        }
        else
        {
            orderNumber = $"SO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        }

        // 6. Instantiate Domain Entity
        var salesOrder = new SalesOrder(orderNumber, customer.Id, warehouse.Id);

        foreach (var item in request.Items)
        {
            salesOrder.AddItem(item.ProductId, item.Quantity, item.UnitPrice);
        }

        _context.SalesOrders.Add(salesOrder);
        await _context.SaveChangesAsync(cancellationToken);

        var itemDtos = salesOrder.Items.Select(i =>
        {
            var prod = products[i.ProductId];
            return new SalesOrderItemDto(
                i.Id,
                i.ProductId,
                prod.Name,
                prod.SKU,
                i.Quantity,
                i.UnitPrice,
                i.TotalPrice
            );
        }).ToList();

        return new SalesOrderDto(
            salesOrder.Id,
            salesOrder.OrderNumber,
            customer.Id,
            customer.Name,
            warehouse.Id,
            warehouse.Name,
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
