using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Suppliers.DTOs;

namespace WarehouseManagement.Application.Features.Suppliers.Commands.UpdateSupplier;

public class UpdateSupplierCommandHandler : IRequestHandler<UpdateSupplierCommand, SupplierDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateSupplierCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SupplierDto> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.PurchaseOrders)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (supplier == null)
        {
            throw new KeyNotFoundException($"Supplier with ID {request.Id} was not found.");
        }

        var trimmedName = request.Name.Trim();
        var trimmedEmail = request.Email.Trim().ToLowerInvariant();

        var duplicateEmail = await _context.Suppliers
            .AnyAsync(s => s.Id != request.Id && s.Email.ToLower() == trimmedEmail, cancellationToken);

        if (duplicateEmail)
        {
            throw new InvalidOperationException($"Another supplier already exists with email '{request.Email}'.");
        }

        supplier.Update(
            trimmedName,
            trimmedEmail,
            request.PhoneNumber.Trim(),
            request.ContactPerson?.Trim(),
            request.Address?.Trim());

        await _context.SaveChangesAsync(cancellationToken);

        return new SupplierDto(
            supplier.Id,
            supplier.Name,
            supplier.ContactPerson,
            supplier.Email,
            supplier.PhoneNumber,
            supplier.Address,
            supplier.IsActive,
            supplier.PurchaseOrders.Count,
            supplier.CreatedAt
        );
    }
}
