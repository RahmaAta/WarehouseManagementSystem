using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Infrastructure.Services;

/// <summary>
/// Generates cryptographically signed JWT tokens with user and role claims.
/// </summary>
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(User user, string roleName)
    {
        var secret = _configuration["JwtSettings:Secret"] 
            ?? "DefaultWarehouseManagementSecretKeyMustBe32CharsLong!";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "WarehouseManagementAPI";
        var audience = _configuration["JwtSettings:Audience"] ?? "WarehouseManagementClients";
        var expiryMinutesStr = _configuration["JwtSettings:ExpiryMinutes"] ?? "120";

        if (!int.TryParse(expiryMinutesStr, out var expiryMinutes))
        {
            expiryMinutes = 120;
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, roleName),
            new("FullName", user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return (tokenHandler.WriteToken(token), expiresAt);
    }
}
