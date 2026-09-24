namespace WarehouseManagement.Application.Common.Interfaces;

/// <summary>
/// Cryptographic hashing contract for securing user passwords.
/// </summary>
public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
