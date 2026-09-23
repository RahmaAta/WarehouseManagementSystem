using WarehouseManagement.Domain.Common;
using WarehouseManagement.Domain.Enums;
using WarehouseManagement.Domain.Exceptions;

namespace WarehouseManagement.Domain.Entities;

/// <summary>
/// Purchase order placed with an external supplier to replenish warehouse inventory.
/// </summary>
public class PurchaseOrder : AuditableEntity
{
    public string OrderNumber { get; private set; } = string.Empty;
    public int SupplierId { get; private set; }
    public int WarehouseId { get; private set; }
    public PurchaseOrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTime? ReceivedAt { get; private set; }
    public string? ReceivedBy { get; private set; }

    // Navigation properties
    public Supplier? Supplier { get; private set; }
    public Warehouse? Warehouse { get; private set; }

    private readonly List<PurchaseOrderItem> _items = new();
    public IReadOnlyCollection<PurchaseOrderItem> Items => _items.AsReadOnly();

    protected PurchaseOrder() { }

    public PurchaseOrder(string orderNumber, int supplierId, int warehouseId)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new ArgumentException("Order number cannot be null or whitespace.", nameof(orderNumber));
        if (supplierId <= 0)
            throw new ArgumentException("Supplier ID must be greater than zero.", nameof(supplierId));
        if (warehouseId <= 0)
            throw new ArgumentException("Warehouse ID must be greater than zero.", nameof(warehouseId));

        OrderNumber = orderNumber.Trim().ToUpperInvariant();
        SupplierId = supplierId;
        WarehouseId = warehouseId;
        Status = PurchaseOrderStatus.Draft;
        TotalAmount = 0m;
    }

    public void AddItem(int productId, int quantity, decimal unitPrice)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Cannot add items to a purchase order that is not in Draft status.");

        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            throw new InvalidOperationException($"Product ID {productId} is already present in this purchase order.");
        }

        var item = new PurchaseOrderItem(productId, quantity, unitPrice);
        _items.Add(item);
        RecalculateTotal();
    }

    public void RemoveItem(int productId)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Cannot remove items from a purchase order that is not in Draft status.");

        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            _items.Remove(item);
            RecalculateTotal();
        }
    }

    public void SubmitForApproval()
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOrderStateException(nameof(PurchaseOrder), Status.ToString(), PurchaseOrderStatus.PendingApproval.ToString());

        if (!_items.Any())
            throw new InvalidOperationException("Cannot submit a purchase order without items.");

        Status = PurchaseOrderStatus.PendingApproval;
    }

    public void Approve(string approvedBy)
    {
        if (Status != PurchaseOrderStatus.PendingApproval && Status != PurchaseOrderStatus.Draft)
            throw new InvalidOrderStateException(nameof(PurchaseOrder), Status.ToString(), PurchaseOrderStatus.Approved.ToString());

        if (!_items.Any())
            throw new InvalidOperationException("Cannot approve a purchase order without items.");

        Status = PurchaseOrderStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ApprovedBy = approvedBy;
    }

    public void MarkAsReceived(string receivedBy)
    {
        // Business Rule: Purchase order MUST be approved before receiving.
        // Direct transition from PendingApproval/Draft to Received is strictly forbidden.
        if (Status != PurchaseOrderStatus.Approved)
            throw new InvalidOrderStateException(nameof(PurchaseOrder), Status.ToString(), PurchaseOrderStatus.Received.ToString());

        Status = PurchaseOrderStatus.Received;
        ReceivedAt = DateTime.UtcNow;
        ReceivedBy = receivedBy;
    }

    public void Cancel()
    {
        if (Status == PurchaseOrderStatus.Received)
            throw new InvalidOperationException("Cannot cancel a purchase order that has already been received.");

        if (Status == PurchaseOrderStatus.Cancelled)
            return;

        Status = PurchaseOrderStatus.Cancelled;
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Sum(item => item.TotalPrice);
    }
}
