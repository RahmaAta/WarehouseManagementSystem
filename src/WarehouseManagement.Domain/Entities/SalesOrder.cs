using WarehouseManagement.Domain.Common;
using WarehouseManagement.Domain.Enums;
using WarehouseManagement.Domain.Exceptions;

namespace WarehouseManagement.Domain.Entities;

/// <summary>
/// Customer sales order fulfilled by deducting available stock from warehouse inventory.
/// </summary>
public class SalesOrder : AuditableEntity
{
    public string OrderNumber { get; private set; } = string.Empty;
    public int CustomerId { get; private set; }
    public int WarehouseId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public string? ConfirmedBy { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? CompletedBy { get; private set; }

    // Navigation properties
    public Customer? Customer { get; private set; }
    public Warehouse? Warehouse { get; private set; }

    private readonly List<SalesOrderItem> _items = new();
    public IReadOnlyCollection<SalesOrderItem> Items => _items.AsReadOnly();

    protected SalesOrder() { }

    public SalesOrder(string orderNumber, int customerId, int warehouseId)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new ArgumentException("Order number cannot be null or whitespace.", nameof(orderNumber));
        if (customerId <= 0)
            throw new ArgumentException("Customer ID must be greater than zero.", nameof(customerId));
        if (warehouseId <= 0)
            throw new ArgumentException("Warehouse ID must be greater than zero.", nameof(warehouseId));

        OrderNumber = orderNumber.Trim().ToUpperInvariant();
        CustomerId = customerId;
        WarehouseId = warehouseId;
        Status = OrderStatus.Pending;
        TotalAmount = 0m;
    }

    public void AddItem(int productId, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot add items to an order that is not in Pending status.");

        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            throw new InvalidOperationException($"Product ID {productId} is already present in this sales order.");
        }

        var item = new SalesOrderItem(productId, quantity, unitPrice);
        _items.Add(item);
        RecalculateTotal();
    }

    public void RemoveItem(int productId)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot remove items from an order that is not in Pending status.");

        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            _items.Remove(item);
            RecalculateTotal();
        }
    }

    public void Confirm(string confirmedBy)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOrderStateException(nameof(SalesOrder), Status.ToString(), OrderStatus.Confirmed.ToString());

        if (!_items.Any())
            throw new InvalidOperationException("Cannot confirm a sales order without items.");

        Status = OrderStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        ConfirmedBy = confirmedBy;
    }

    public void Complete(string completedBy)
    {
        // Business Rule: A cancelled or pending order cannot be completed.
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOrderStateException(nameof(SalesOrder), Status.ToString(), OrderStatus.Completed.ToString());

        Status = OrderStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        CompletedBy = completedBy;
    }

    public void Cancel()
    {
        // Business Rule: A completed order cannot be cancelled directly.
        if (Status == OrderStatus.Completed)
            throw new InvalidOperationException("Cannot cancel an order that has already been completed.");

        if (Status == OrderStatus.Cancelled)
            return;

        Status = OrderStatus.Cancelled;
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Sum(item => item.TotalPrice);
    }
}
