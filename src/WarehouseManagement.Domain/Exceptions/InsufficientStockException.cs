namespace WarehouseManagement.Domain.Exceptions;

/// <summary>
/// Thrown when an inventory deduction or order confirmation exceeds available on-hand stock.
/// </summary>
public class InsufficientStockException : DomainException
{
    public int ProductId { get; }
    public int WarehouseId { get; }
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }

    public InsufficientStockException(int productId, int warehouseId, int requestedQuantity, int availableQuantity)
        : base($"Insufficient stock for Product ID {productId} in Warehouse ID {warehouseId}. Requested: {requestedQuantity}, Available: {availableQuantity}.")
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        RequestedQuantity = requestedQuantity;
        AvailableQuantity = availableQuantity;
    }
}
