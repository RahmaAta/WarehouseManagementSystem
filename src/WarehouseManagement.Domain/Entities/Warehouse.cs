using WarehouseManagement.Domain.Common;

namespace WarehouseManagement.Domain.Entities;

/// <summary>
/// Physical storage facility managing multi-location stock.
/// </summary>
public class Warehouse : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    // Navigation property
    private readonly List<InventoryItem> _inventoryItems = new();
    public IReadOnlyCollection<InventoryItem> InventoryItems => _inventoryItems.AsReadOnly();

    protected Warehouse() { }

    public Warehouse(string name, string location)
    {
        SetName(name);
        SetLocation(location);
        IsActive = true;
    }

    public void Update(string name, string location)
    {
        SetName(name);
        SetLocation(location);
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
            throw new ArgumentException("Warehouse name cannot be null or whitespace.", nameof(name));

        Name = name.Trim();
    }

    private void SetLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Warehouse location cannot be null or whitespace.", nameof(location));

        Location = location.Trim();
    }
}
