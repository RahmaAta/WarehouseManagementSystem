using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WarehouseManagement.Application.Common.Interfaces;

namespace WarehouseManagement.Application.Features.Products.Commands.DeleteProduct
{
    public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public DeleteProductCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .Include(p => p.InventoryItems)
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID {request.Id} was not found.");
            }

            // Business Rule: Cannot deactivate/delete a product that still has physical stock in warehouse
            var onHandStock = product.InventoryItems.Sum(i => i.Quantity);
            if (onHandStock > 0)
            {
                throw new InvalidOperationException(
                    $"Cannot deactivate product '{product.Name}' (SKU: {product.SKU}) because it has {onHandStock} units currently on hand across warehouse locations. Deplete or transfer stock first.");
            }

            // Enterprise pattern: Soft delete / deactivation preserves foreign key references in historical orders & ledger
            product.Deactivate();

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }

}
