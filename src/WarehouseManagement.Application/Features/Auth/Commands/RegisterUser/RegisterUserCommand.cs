using MediatR;
using WarehouseManagement.Application.Features.Auth.DTOs;

namespace WarehouseManagement.Application.Features.Auth.Commands.RegisterUser;

public record RegisterUserCommand(
    string Username,
    string Email,
    string Password,
    string FullName,
    int RoleId
) : IRequest<AuthResponseDto>;
