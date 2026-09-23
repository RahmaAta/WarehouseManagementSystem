using WarehouseManagement.Domain.Common;

namespace WarehouseManagement.Domain.Entities;

/// <summary>
/// Client placing sales orders fulfilled from warehouse stock.
/// </summary>
public class Customer : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation property
    private readonly List<SalesOrder> _salesOrders = new();
    public IReadOnlyCollection<SalesOrder> SalesOrders => _salesOrders.AsReadOnly();

    protected Customer() { }

    public Customer(string name, string email, string phoneNumber, string? address = null)
    {
        SetName(name);
        Email = email?.Trim() ?? string.Empty;
        PhoneNumber = phoneNumber?.Trim() ?? string.Empty;
        Address = address?.Trim();
        IsActive = true;
    }

    public void Update(string name, string email, string phoneNumber, string? address)
    {
        SetName(name);
        Email = email?.Trim() ?? string.Empty;
        PhoneNumber = phoneNumber?.Trim() ?? string.Empty;
        Address = address?.Trim();
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
            throw new ArgumentException("Customer name cannot be null or whitespace.", nameof(name));

        Name = name.Trim();
    }
}
