using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Customers.DTOs;

namespace WarehouseManagement.Application.Features.Customers.Queries.GetCustomerById;

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    private readonly IApplicationDbContext _context;

    public GetCustomerByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new CustomerDto(
                c.Id,
                c.Name,
                c.Email,
                c.PhoneNumber,
                c.Address,
                c.IsActive,
                c.SalesOrders.Count,
                c.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (customer == null)
        {
            throw new KeyNotFoundException($"Customer with ID {request.Id} was not found.");
        }

        return customer;
    }
}
