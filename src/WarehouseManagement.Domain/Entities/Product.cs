using WarehouseManagement.Domain.Common;
using WarehouseManagement.Domain.Exceptions;

namespace WarehouseManagement.Domain.Entities;

/// <summary>
/// Catalog product managed across inventory and warehouse locations.
/// </summary>
public class Product : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string SKU { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public int MinimumStockLevel { get; private set; }
    public int CategoryId { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public Category? Category { get; private set; }

    private readonly List<InventoryItem> _inventoryItems = new();
    public IReadOnlyCollection<InventoryItem> InventoryItems => _inventoryItems.AsReadOnly();

    private readonly List<StockTransaction> _stockTransactions = new();
    public IReadOnlyCollection<StockTransaction> StockTransactions => _stockTransactions.AsReadOnly();

    protected Product() { }

    public Product(string name, string sku, decimal price, int minimumStockLevel, int categoryId, string? description = null)
    {
        SetName(name);
        SetSKU(sku);
        SetPrice(price);
        SetMinimumStockLevel(minimumStockLevel);
        CategoryId = categoryId;
        Description = description;
        IsActive = true;
    }

    public void Update(string name, string sku, decimal price, int minimumStockLevel, int categoryId, string? description)
    {
        SetName(name);
        SetSKU(sku);
        SetPrice(price);
        SetMinimumStockLevel(minimumStockLevel);
        CategoryId = categoryId;
        Description = description;
    }

    public void SetPrice(decimal price)
    {
        if (price < 0)
            throw new NegativePriceException(price);

        Price = price;
    }

    public void SetMinimumStockLevel(int level)
    {
        if (level < 0)
            throw new ArgumentException("Minimum stock level cannot be negative.", nameof(level));

        MinimumStockLevel = level;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or whitespace.", nameof(name));

        Name = name.Trim();
    }

    private void SetSKU(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("Product SKU cannot be null or whitespace.", nameof(sku));

        SKU = sku.Trim().ToUpperInvariant();
    }
}
