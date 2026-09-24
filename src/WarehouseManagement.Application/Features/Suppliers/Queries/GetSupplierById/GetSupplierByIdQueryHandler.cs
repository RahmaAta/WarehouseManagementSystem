using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Suppliers.DTOs;

namespace WarehouseManagement.Application.Features.Suppliers.Queries.GetSupplierById;

public class GetSupplierByIdQueryHandler : IRequestHandler<GetSupplierByIdQuery, SupplierDto>
{
    private readonly IApplicationDbContext _context;

    public GetSupplierByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SupplierDto> Handle(GetSupplierByIdQuery request, CancellationToken cancellationToken)
    {
        var supplier = await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.Id == request.Id)
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
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (supplier == null)
        {
            throw new KeyNotFoundException($"Supplier with ID {request.Id} was not found.");
        }

        return supplier;
    }
}
