using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Customers.DTOs;

namespace WarehouseManagement.Application.Features.Customers.Commands.UpdateCustomer;

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateCustomerCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .Include(c => c.SalesOrders)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (customer == null)
        {
            throw new KeyNotFoundException($"Customer with ID {request.Id} was not found.");
        }

        var trimmedName = request.Name.Trim();
        var trimmedEmail = request.Email.Trim().ToLowerInvariant();

        var duplicateEmail = await _context.Customers
            .AnyAsync(c => c.Id != request.Id && c.Email.ToLower() == trimmedEmail, cancellationToken);

        if (duplicateEmail)
        {
            throw new InvalidOperationException($"Another customer already exists with email '{request.Email}'.");
        }

        customer.Update(
            trimmedName,
            trimmedEmail,
            request.PhoneNumber.Trim(),
            request.Address?.Trim());

        await _context.SaveChangesAsync(cancellationToken);

        return new CustomerDto(
            customer.Id,
            customer.Name,
            customer.Email,
            customer.PhoneNumber,
            customer.Address,
            customer.IsActive,
            customer.SalesOrders.Count,
            customer.CreatedAt
        );
    }
}
