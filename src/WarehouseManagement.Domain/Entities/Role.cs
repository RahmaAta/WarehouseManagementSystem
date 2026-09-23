using WarehouseManagement.Domain.Common;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Domain.Entities;

/// <summary>
/// Security authorization role assigned to users.
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    // Navigation property
    private readonly List<User> _users = new();
    public IReadOnlyCollection<User> Users => _users.AsReadOnly();

    protected Role() { }

    public Role(UserRoleType roleType, string? description = null)
    {
        Name = roleType.ToString();
        Description = description;
    }

    public Role(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be null or whitespace.", nameof(name));

        Name = name.Trim();
        Description = description;
    }
}
