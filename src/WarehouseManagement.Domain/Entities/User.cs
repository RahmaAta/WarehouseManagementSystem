using WarehouseManagement.Domain.Common;

namespace WarehouseManagement.Domain.Entities;

/// <summary>
/// Authenticated user of the warehouse management system.
/// </summary>
public class User : AuditableEntity
{
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public int RoleId { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation property
    public Role? Role { get; private set; }

    protected User() { }

    public User(string username, string email, string passwordHash, string fullName, int roleId)
    {
        SetUsername(username);
        SetEmail(email);
        SetPasswordHash(passwordHash);
        FullName = fullName?.Trim() ?? string.Empty;
        RoleId = roleId;
        IsActive = true;
    }

    public void UpdateProfile(string fullName, string email, int roleId)
    {
        FullName = fullName?.Trim() ?? string.Empty;
        SetEmail(email);
        RoleId = roleId;
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be null or whitespace.", nameof(passwordHash));

        PasswordHash = passwordHash;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    private void SetUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be null or whitespace.", nameof(username));

        Username = username.Trim().ToLowerInvariant();
    }

    private void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or whitespace.", nameof(email));

        Email = email.Trim().ToLowerInvariant();
    }
}
