using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;

namespace WarehouseManagement.Application.Features.Suppliers.Commands.ActivateSupplier;

public class ActivateSupplierCommandHandler : IRequestHandler<ActivateSupplierCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public ActivateSupplierCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ActivateSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (supplier == null)
        {
            throw new KeyNotFoundException($"Supplier with ID {request.Id} was not found.");
        }

        supplier.Activate();
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
