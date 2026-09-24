using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;

namespace WarehouseManagement.Application.Features.Suppliers.Commands.DeleteSupplier;

public class DeleteSupplierCommandHandler : IRequestHandler<DeleteSupplierCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteSupplierCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (supplier == null)
        {
            throw new KeyNotFoundException($"Supplier with ID {request.Id} was not found.");
        }

        // Soft deletion preserves historical purchase orders and financial ledger references
        supplier.Deactivate();

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
