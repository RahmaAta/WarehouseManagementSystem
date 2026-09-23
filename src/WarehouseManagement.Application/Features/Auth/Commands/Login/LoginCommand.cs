using MediatR;
using WarehouseManagement.Application.Features.Auth.DTOs;

namespace WarehouseManagement.Application.Features.Auth.Commands.Login;

public record LoginCommand(
    string Username,
    string Password
) : IRequest<AuthResponseDto>;
