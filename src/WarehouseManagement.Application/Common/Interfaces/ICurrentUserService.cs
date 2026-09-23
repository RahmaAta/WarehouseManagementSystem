namespace WarehouseManagement.Application.Common.Interfaces;

/// <summary>
/// Contract providing access to the current authenticated caller context.
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? Username { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
