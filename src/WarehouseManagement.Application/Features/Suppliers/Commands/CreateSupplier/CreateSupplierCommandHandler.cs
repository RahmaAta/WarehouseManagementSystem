using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Suppliers.DTOs;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Features.Suppliers.Commands.CreateSupplier;

public class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, SupplierDto>
{
    private readonly IApplicationDbContext _context;

    public CreateSupplierCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SupplierDto> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        var trimmedName = request.Name.Trim();
        var trimmedEmail = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _context.Suppliers
            .AnyAsync(s => s.Email.ToLower() == trimmedEmail, cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException($"Supplier with email '{request.Email}' already exists.");
        }

        var supplier = new Supplier(
            trimmedName,
            trimmedEmail,
            request.PhoneNumber.Trim(),
            request.ContactPerson?.Trim(),
            request.Address?.Trim());

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync(cancellationToken);

        return new SupplierDto(
            supplier.Id,
            supplier.Name,
            supplier.ContactPerson,
            supplier.Email,
            supplier.PhoneNumber,
            supplier.Address,
            supplier.IsActive,
            TotalPurchaseOrdersCount: 0,
            supplier.CreatedAt
        );
    }
}
