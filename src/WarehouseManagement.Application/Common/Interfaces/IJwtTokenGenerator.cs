using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Common.Interfaces;

/// <summary>
/// Contract for issuing signed JSON Web Tokens (JWT) for authenticated users.
/// </summary>
public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user, string roleName);
}
