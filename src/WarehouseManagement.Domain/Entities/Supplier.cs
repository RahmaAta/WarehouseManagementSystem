using WarehouseManagement.Domain.Common;

namespace WarehouseManagement.Domain.Entities;

/// <summary>
/// External vendor supplying products via purchase orders.
/// </summary>
public class Supplier : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? ContactPerson { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation property
    private readonly List<PurchaseOrder> _purchaseOrders = new();
    public IReadOnlyCollection<PurchaseOrder> PurchaseOrders => _purchaseOrders.AsReadOnly();

    protected Supplier() { }

    public Supplier(string name, string email, string phoneNumber, string? contactPerson = null, string? address = null)
    {
        SetName(name);
        Email = email?.Trim() ?? string.Empty;
        PhoneNumber = phoneNumber?.Trim() ?? string.Empty;
        ContactPerson = contactPerson?.Trim();
        Address = address?.Trim();
        IsActive = true;
    }

    public void Update(string name, string email, string phoneNumber, string? contactPerson, string? address)
    {
        SetName(name);
        Email = email?.Trim() ?? string.Empty;
        PhoneNumber = phoneNumber?.Trim() ?? string.Empty;
        ContactPerson = contactPerson?.Trim();
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
            throw new ArgumentException("Supplier name cannot be null or whitespace.", nameof(name));

        Name = name.Trim();
    }
}
