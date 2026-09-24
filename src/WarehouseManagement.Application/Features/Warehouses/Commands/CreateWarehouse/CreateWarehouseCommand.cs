using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Warehouses.DTOs;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Features.Warehouses.Commands.CreateWarehouse;

public record CreateWarehouseCommand(string Name, string Location) : IRequest<WarehouseDto>;

public class CreateWarehouseCommandHandler : IRequestHandler<CreateWarehouseCommand, WarehouseDto>
{
    private readonly IApplicationDbContext _context;

    public CreateWarehouseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WarehouseDto> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var trimmedName = request.Name.Trim();
        var trimmedLocation = request.Location.Trim();

        var exists = await _context.Warehouses
            .AnyAsync(w => w.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"Warehouse with name '{request.Name}' already exists.");
        }

        var warehouse = new Warehouse(trimmedName, trimmedLocation);

        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync(cancellationToken);

        return new WarehouseDto(
            warehouse.Id,
            warehouse.Name,
            warehouse.Location,
            warehouse.IsActive,
            TotalDistinctProducts: 0,
            TotalStockUnits: 0,
            warehouse.CreatedAt
        );
    }
}
