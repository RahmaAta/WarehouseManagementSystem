using WarehouseManagement.Domain.Common;
using WarehouseManagement.Domain.Enums;
using WarehouseManagement.Domain.Exceptions;

namespace WarehouseManagement.Domain.Entities;

/// <summary>
/// Immutable audit log capturing every physical stock addition, deduction, or transfer.
/// </summary>
public class StockTransaction : BaseEntity
{
    public int ProductId { get; private set; }
    public int WarehouseId { get; private set; }
    public int? ToWarehouseId { get; private set; }
    public StockTransactionType Type { get; private set; }
    public int Quantity { get; private set; }
    public string? ReferenceId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public string? CreatedBy { get; private set; }

    // Navigation properties
    public Product? Product { get; private set; }
    public Warehouse? Warehouse { get; private set; }
    public Warehouse? ToWarehouse { get; private set; }

    protected StockTransaction() { }

    public static StockTransaction CreateStockIn(int productId, int warehouseId, int quantity, string? referenceId, string? notes, string? createdBy = null)
    {
        ValidateInputs(productId, warehouseId, quantity);

        return new StockTransaction
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Type = StockTransactionType.StockIn,
            Quantity = quantity,
            ReferenceId = referenceId?.Trim(),
            Notes = notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public static StockTransaction CreateStockOut(int productId, int warehouseId, int quantity, string? referenceId, string? notes, string? createdBy = null)
    {
        ValidateInputs(productId, warehouseId, quantity);

        return new StockTransaction
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Type = StockTransactionType.StockOut,
            Quantity = quantity,
            ReferenceId = referenceId?.Trim(),
            Notes = notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public static StockTransaction CreateTransfer(int productId, int fromWarehouseId, int toWarehouseId, int quantity, string? referenceId, string? notes, string? createdBy = null)
    {
        ValidateInputs(productId, fromWarehouseId, quantity);

        if (fromWarehouseId == toWarehouseId)
            throw new SameWarehouseTransferException(fromWarehouseId);

        return new StockTransaction
        {
            ProductId = productId,
            WarehouseId = fromWarehouseId,
            ToWarehouseId = toWarehouseId,
            Type = StockTransactionType.Transfer,
            Quantity = quantity,
            ReferenceId = referenceId?.Trim(),
            Notes = notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    private static void ValidateInputs(int productId, int warehouseId, int quantity)
    {
        if (productId <= 0)
            throw new ArgumentException("Product ID must be greater than zero.", nameof(productId));
        if (warehouseId <= 0)
            throw new ArgumentException("Warehouse ID must be greater than zero.", nameof(warehouseId));
        if (quantity <= 0)
            throw new ArgumentException("Transaction quantity must be greater than zero.", nameof(quantity));
    }
}
