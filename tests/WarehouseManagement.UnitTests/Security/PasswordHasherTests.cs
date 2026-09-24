using WarehouseManagement.Infrastructure.Services;
using Xunit;

namespace WarehouseManagement.UnitTests.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ShouldProduceDifferentHash_ForSamePassword_DueToUniqueSalt()
    {
        // Arrange
        const string password = "SecretPassword123!";

        // Act
        var hash1 = _hasher.HashPassword(password);
        var hash2 = _hasher.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2);
        Assert.Contains(".", hash1);
        Assert.Contains(".", hash2);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        const string password = "StrongPassword#2026";
        var hash = _hasher.HashPassword(password);

        // Act
        var result = _hasher.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        const string password = "CorrectPassword123";
        const string wrongPassword = "WrongPassword123";
        var hash = _hasher.HashPassword(password);

        // Act
        var result = _hasher.VerifyPassword(wrongPassword, hash);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void VerifyPassword_WithInvalidInputs_ShouldReturnFalse(string? invalidInput)
    {
        // Arrange
        var hash = _hasher.HashPassword("ValidPassword123");

        // Act & Assert
        Assert.False(_hasher.VerifyPassword(invalidInput!, hash));
        Assert.False(_hasher.VerifyPassword("ValidPassword123", invalidInput!));
    }
}
