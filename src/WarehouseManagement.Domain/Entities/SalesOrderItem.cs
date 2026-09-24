using WarehouseManagement.Domain.Common;
using WarehouseManagement.Domain.Exceptions;

namespace WarehouseManagement.Domain.Entities;

/// <summary>
/// Line item in a customer sales order.
/// </summary>
public class SalesOrderItem : BaseEntity
{
    public int SalesOrderId { get; private set; }
    public int ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal TotalPrice => Quantity * UnitPrice;

    // Navigation properties
    public SalesOrder? SalesOrder { get; private set; }
    public Product? Product { get; private set; }

    protected SalesOrderItem() { }

    public SalesOrderItem(int productId, int quantity, decimal unitPrice)
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
    }
}
