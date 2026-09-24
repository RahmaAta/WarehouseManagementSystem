using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Products.DTOs;

namespace WarehouseManagement.Application.Features.Products.Commands.UpdateProduct
{
    public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
    {
        private readonly IApplicationDbContext _context;

        public UpdateProductCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.InventoryItems)
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID {request.Id} was not found.");
            }

            // Validate Category
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

            if (category == null)
            {
                throw new KeyNotFoundException($"Category with ID {request.CategoryId} does not exist.");
            }

            // Validate SKU uniqueness across other products
            var normalizedSku = request.SKU.Trim().ToUpperInvariant();
            var skuDuplicate = await _context.Products
                .AnyAsync(p => p.Id != request.Id && p.SKU == normalizedSku, cancellationToken);

            if (skuDuplicate)
            {
                throw new InvalidOperationException($"Another product already exists with SKU '{normalizedSku}'.");
            }

            product.Update(
                request.Name,
                normalizedSku,
                request.Price,
                request.MinimumStockLevel,
                request.CategoryId,
                request.Description);

            await _context.SaveChangesAsync(cancellationToken);

            var totalStock = product.InventoryItems.Sum(i => i.AvailableQuantity);

            return new ProductDto(
                product.Id,
                product.Name,
                product.SKU,
                product.Description,
                product.Price,
                product.MinimumStockLevel,
                product.CategoryId,
                category.Name,
                product.IsActive,
                product.CreatedAt,
                product.LastModifiedAt,
                totalStock);
        }
    }

}
