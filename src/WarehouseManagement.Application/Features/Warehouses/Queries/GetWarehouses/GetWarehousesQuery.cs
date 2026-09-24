using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Warehouses.DTOs;

namespace WarehouseManagement.Application.Features.Warehouses.Queries.GetWarehouses;

public record GetWarehousesQuery(bool IncludeInactive = false) : IRequest<List<WarehouseDto>>;

public class GetWarehousesQueryHandler : IRequestHandler<GetWarehousesQuery, List<WarehouseDto>>
{
    private readonly IApplicationDbContext _context;

    public GetWarehousesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WarehouseDto>> Handle(GetWarehousesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Warehouses.AsNoTracking();

        if (!request.IncludeInactive)
        {
            query = query.Where(w => w.IsActive);
        }

        return await query
            .OrderBy(w => w.Name)
            .Select(w => new WarehouseDto(
                w.Id,
                w.Name,
                w.Location,
                w.IsActive,
                w.InventoryItems.Count(i => i.Quantity > 0),
                w.InventoryItems.Sum(i => (int?)i.Quantity) ?? 0,
                w.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}
