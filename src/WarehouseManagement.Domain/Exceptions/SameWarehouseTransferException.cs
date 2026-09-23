namespace WarehouseManagement.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to transfer stock where source and destination warehouses are identical.
/// </summary>
public class SameWarehouseTransferException : DomainException
{
    public int WarehouseId { get; }

    public SameWarehouseTransferException(int warehouseId)
        : base($"Stock transfer cannot happen between the same warehouse. Warehouse ID: {warehouseId}.")
    {
        WarehouseId = warehouseId;
    }
}
