using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;

namespace WarehouseManagement.Application.Features.Warehouses.Commands.DeleteWarehouse;

public record DeleteWarehouseCommand(int Id) : IRequest<bool>;

public class DeleteWarehouseCommandHandler : IRequestHandler<DeleteWarehouseCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteWarehouseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await _context.Warehouses
            .Include(w => w.InventoryItems)
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);

        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with ID {request.Id} was not found.");
        }

        // Business Rule: Cannot deactivate/delete a warehouse that currently stores physical items
        var totalStock = warehouse.InventoryItems.Sum(i => i.Quantity);
        if (totalStock > 0)
        {
            throw new InvalidOperationException(
                $"Cannot deactivate warehouse '{warehouse.Name}' because it currently stores {totalStock} units of inventory. Transfer or deplete all stock before deactivating.");
        }

        warehouse.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
