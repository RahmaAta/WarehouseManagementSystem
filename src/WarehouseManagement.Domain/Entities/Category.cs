using WarehouseManagement.Domain.Common;

namespace WarehouseManagement.Domain.Entities;

/// <summary>
/// Categorization grouping for catalog products.
/// </summary>
public class Category : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation property
    private readonly List<Product> _products = new();
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    // EF Core parameterless constructor
    protected Category() { }

    public Category(string name, string? description = null)
    {
        SetName(name);
        Description = description;
        IsActive = true;
    }

    public void Update(string name, string? description)
    {
        SetName(name);
        Description = description;
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
            throw new ArgumentException("Category name cannot be null or whitespace.", nameof(name));

        Name = name.Trim();
    }
}
