using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.Suppliers.DTOs;

namespace WarehouseManagement.Application.Features.Suppliers.Queries.GetSuppliers;

public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, PaginatedList<SupplierDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSuppliersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<SupplierDto>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 10 : (request.PageSize > 100 ? 100 : request.PageSize);

        var query = _context.Suppliers.AsNoTracking();

        if (!request.IncludeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(s => s.Name.Contains(term) 
                || s.Email.Contains(term) 
                || (s.ContactPerson != null && s.ContactPerson.Contains(term)));
        }

        var projected = query
            .OrderBy(s => s.Name)
            .Select(s => new SupplierDto(
                s.Id,
                s.Name,
                s.ContactPerson,
                s.Email,
                s.PhoneNumber,
                s.Address,
                s.IsActive,
                s.PurchaseOrders.Count,
                s.CreatedAt
            ));

        return await PaginatedList<SupplierDto>.CreateAsync(projected, pageNumber, pageSize, cancellationToken);
    }
}
