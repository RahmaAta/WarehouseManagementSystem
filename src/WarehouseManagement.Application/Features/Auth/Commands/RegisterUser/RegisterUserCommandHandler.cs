using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Auth.DTOs;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Features.Auth.Commands.RegisterUser;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterUserCommandHandler(
        IApplicationDbContext _context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        this._context = _context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponseDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var usernameExists = await _context.Users
            .AnyAsync(u => u.Username == normalizedUsername, cancellationToken);

        if (usernameExists)
        {
            throw new InvalidOperationException($"Username '{request.Username}' is already taken.");
        }

        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");
        }

        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        if (role == null)
        {
            throw new InvalidOperationException($"Role ID {request.RoleId} does not exist.");
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);

        var user = new User(
            normalizedUsername,
            normalizedEmail,
            passwordHash,
            request.FullName,
            role.Id);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user, role.Name);

        return new AuthResponseDto(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            role.Name,
            token,
            expiresAt);
    }
}
