using WarehouseManagement.Domain.Common;
using WarehouseManagement.Domain.Exceptions;

namespace WarehouseManagement.Domain.Entities;

/// <summary>
/// Line item in a supplier purchase order.
/// </summary>
public class PurchaseOrderItem : BaseEntity
{
    public int PurchaseOrderId { get; private set; }
    public int ProductId { get; private set; }
    public int Quantity { get; private set; }
    public int ReceivedQuantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal TotalPrice => Quantity * UnitPrice;

    // Navigation properties
    public PurchaseOrder? PurchaseOrder { get; private set; }
    public Product? Product { get; private set; }

    protected PurchaseOrderItem() { }

    public PurchaseOrderItem(int productId, int quantity, decimal unitPrice)
    {
        if (productId <= 0)
            throw new ArgumentException("Product ID must be greater than zero.", nameof(productId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (unitPrice < 0)
            throw new NegativePriceException(unitPrice);

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        ReceivedQuantity = 0;
    }

    public void RecordReceivedQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Received quantity must be greater than zero.", nameof(quantity));

        if (ReceivedQuantity + quantity > Quantity)
            throw new InvalidOperationException($"Cannot receive {quantity} items. Total received would exceed ordered quantity ({Quantity}).");

        ReceivedQuantity += quantity;
    }
}
