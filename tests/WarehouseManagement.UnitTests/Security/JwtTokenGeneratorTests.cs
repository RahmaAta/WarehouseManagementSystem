using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Infrastructure.Services;
using Xunit;

namespace WarehouseManagement.UnitTests.Security;

public class JwtTokenGeneratorTests
{
    private readonly JwtTokenGenerator _generator;

    public JwtTokenGeneratorTests()
    {
        var configData = new Dictionary<string, string?>
        {
            { "JwtSettings:Secret", "TestSuperSecretKeyForWarehouseManagementUnitTests2026!" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" },
            { "JwtSettings:ExpiryMinutes", "60" }
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _generator = new JwtTokenGenerator(configuration);
    }

    [Fact]
    public void GenerateToken_ShouldCreateValidJwtWithExpectedClaims()
    {
        // Arrange
        var user = new User("ahmed_manager", "ahmed@warehouse.com", "fakeHash", "Ahmed Ali", roleId: 2);
        typeof(User).GetProperty("Id")!.SetValue(user, 42);

        // Act
        var (token, expiresAt) = _generator.GenerateToken(user, "WarehouseManager");

        // Assert
        Assert.NotNull(token);
        Assert.True(expiresAt > DateTime.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("TestIssuer", jwtToken.Issuer);
        Assert.Contains(jwtToken.Audiences, a => a == "TestAudience");

        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role");
        Assert.NotNull(roleClaim);
        Assert.Equal("WarehouseManager", roleClaim.Value);

        var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "unique_name");
        Assert.NotNull(nameClaim);
        Assert.Equal("ahmed_manager", nameClaim.Value);

        var idClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid");
        Assert.NotNull(idClaim);
        Assert.Equal("42", idClaim.Value);
    }
}
