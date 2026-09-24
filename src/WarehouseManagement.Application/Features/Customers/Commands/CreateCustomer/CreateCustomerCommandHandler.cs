using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Customers.DTOs;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Features.Customers.Commands.CreateCustomer;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly IApplicationDbContext _context;

    public CreateCustomerCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var trimmedName = request.Name.Trim();
        var trimmedEmail = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _context.Customers
            .AnyAsync(c => c.Email.ToLower() == trimmedEmail, cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException($"Customer with email '{request.Email}' already exists.");
        }

        var customer = new Customer(
            trimmedName,
            trimmedEmail,
            request.PhoneNumber.Trim(),
            request.Address?.Trim());

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);

        return new CustomerDto(
            customer.Id,
            customer.Name,
            customer.Email,
            customer.PhoneNumber,
            customer.Address,
            customer.IsActive,
            TotalSalesOrdersCount: 0,
            customer.CreatedAt
        );
    }
}
