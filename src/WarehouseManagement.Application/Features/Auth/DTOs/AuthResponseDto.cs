namespace WarehouseManagement.Application.Features.Auth.DTOs;

public record AuthResponseDto(
    int Id,
    string Username,
    string Email,
    string FullName,
    string Role,
    string Token,
    DateTime ExpiresAt
);

public record UserDto(
    int Id,
    string Username,
    string Email,
    string FullName,
    string Role,
    bool IsActive
);
