using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Warehouses.DTOs;

namespace WarehouseManagement.Application.Features.Warehouses.Queries.GetWarehouseById;

public record GetWarehouseByIdQuery(int Id) : IRequest<WarehouseDto>;

public class GetWarehouseByIdQueryHandler : IRequestHandler<GetWarehouseByIdQuery, WarehouseDto>
{
    private readonly IApplicationDbContext _context;

    public GetWarehouseByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WarehouseDto> Handle(GetWarehouseByIdQuery request, CancellationToken cancellationToken)
    {
        var warehouse = await _context.Warehouses
            .AsNoTracking()
            .Where(w => w.Id == request.Id)
            .Select(w => new WarehouseDto(
                w.Id,
                w.Name,
                w.Location,
                w.IsActive,
                w.InventoryItems.Count(i => i.Quantity > 0),
                w.InventoryItems.Sum(i => (int?)i.Quantity) ?? 0,
                w.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with ID {request.Id} was not found.");
        }

        return warehouse;
    }
}
