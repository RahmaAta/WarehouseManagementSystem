using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.Customers.DTOs;

namespace WarehouseManagement.Application.Features.Customers.Queries.GetCustomers;

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, PaginatedList<CustomerDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCustomersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 10 : (request.PageSize > 100 ? 100 : request.PageSize);

        var query = _context.Customers.AsNoTracking();

        if (!request.IncludeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(c => c.Name.Contains(term) || c.Email.Contains(term) || c.PhoneNumber.Contains(term));
        }

        var projected = query
            .OrderBy(c => c.Name)
            .Select(c => new CustomerDto(
                c.Id,
                c.Name,
                c.Email,
                c.PhoneNumber,
                c.Address,
                c.IsActive,
                c.SalesOrders.Count,
                c.CreatedAt
            ));

        return await PaginatedList<CustomerDto>.CreateAsync(projected, pageNumber, pageSize, cancellationToken);
    }
}
