using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;

namespace WarehouseManagement.Application.Features.Warehouses.Commands.ActivateWarehouse;

public class ActivateWarehouseCommandHandler : IRequestHandler<ActivateWarehouseCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public ActivateWarehouseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ActivateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);

        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with ID {request.Id} was not found.");
        }

        warehouse.Activate();
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
