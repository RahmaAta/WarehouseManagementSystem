using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Warehouses.DTOs;

namespace WarehouseManagement.Application.Features.Warehouses.Commands.UpdateWarehouse;

public record UpdateWarehouseCommand(int Id, string Name, string Location) : IRequest<WarehouseDto>;

public class UpdateWarehouseCommandHandler : IRequestHandler<UpdateWarehouseCommand, WarehouseDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateWarehouseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WarehouseDto> Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await _context.Warehouses
            .Include(w => w.InventoryItems)
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);

        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with ID {request.Id} was not found.");
        }

        var trimmedName = request.Name.Trim();
        var trimmedLocation = request.Location.Trim();

        var duplicateExists = await _context.Warehouses
            .AnyAsync(w => w.Id != request.Id && w.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"Another warehouse already exists with name '{request.Name}'.");
        }

        warehouse.Update(trimmedName, trimmedLocation);
        await _context.SaveChangesAsync(cancellationToken);

        var totalDistinctProducts = warehouse.InventoryItems.Count(i => i.Quantity > 0);
        var totalStockUnits = warehouse.InventoryItems.Sum(i => i.Quantity);

        return new WarehouseDto(
            warehouse.Id,
            warehouse.Name,
            warehouse.Location,
            warehouse.IsActive,
            totalDistinctProducts,
            totalStockUnits,
            warehouse.CreatedAt
        );
    }
}
