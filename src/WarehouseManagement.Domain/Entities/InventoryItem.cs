using WarehouseManagement.Domain.Common;
using WarehouseManagement.Domain.Exceptions;

namespace WarehouseManagement.Domain.Entities;

/// <summary>
/// Tracks stock quantity and reservations for a specific product at a specific warehouse.
/// </summary>
public class InventoryItem : AuditableEntity
{
    public int WarehouseId { get; private set; }
    public int ProductId { get; private set; }
    public int Quantity { get; private set; }
    public int ReservedQuantity { get; private set; }

    /// <summary>
    /// Concurrency token for optimistic concurrency control in EF Core.
    /// </summary>
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    // Calculated property: usable stock free of order allocations
    public int AvailableQuantity => Quantity - ReservedQuantity;

    // Navigation properties
    public Warehouse? Warehouse { get; private set; }
    public Product? Product { get; private set; }

    protected InventoryItem() { }

    public InventoryItem(int warehouseId, int productId, int initialQuantity = 0)
    {
        if (warehouseId <= 0)
            throw new ArgumentException("Warehouse ID must be greater than zero.", nameof(warehouseId));
        if (productId <= 0)
            throw new ArgumentException("Product ID must be greater than zero.", nameof(productId));
        if (initialQuantity < 0)
            throw new ArgumentException("Initial quantity cannot be negative.", nameof(initialQuantity));

        WarehouseId = warehouseId;
        ProductId = productId;
        Quantity = initialQuantity;
        ReservedQuantity = 0;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity to add must be greater than zero.", nameof(quantity));

        Quantity += quantity;
    }

    public void RemoveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity to remove must be greater than zero.", nameof(quantity));

        if (AvailableQuantity < quantity)
            throw new InsufficientStockException(ProductId, WarehouseId, quantity, AvailableQuantity);

        Quantity -= quantity;
    }

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity to reserve must be greater than zero.", nameof(quantity));

        if (AvailableQuantity < quantity)
            throw new InsufficientStockException(ProductId, WarehouseId, quantity, AvailableQuantity);

        ReservedQuantity += quantity;
    }

    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity to release must be greater than zero.", nameof(quantity));

        if (ReservedQuantity < quantity)
            throw new InvalidOperationException($"Cannot release {quantity} items. Only {ReservedQuantity} are reserved.");

        ReservedQuantity -= quantity;
    }

    public void FulfillReservedStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity to fulfill must be greater than zero.", nameof(quantity));

        if (ReservedQuantity < quantity)
            throw new InvalidOperationException($"Cannot fulfill {quantity} items. Only {ReservedQuantity} are reserved.");

        ReservedQuantity -= quantity;
        Quantity -= quantity;
    }
}
