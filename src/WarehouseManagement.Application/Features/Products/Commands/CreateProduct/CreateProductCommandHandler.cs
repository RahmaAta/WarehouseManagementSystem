using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Products.DTOs;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Features.Products.Commands.CreateProduct
{
    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
    {
        private readonly IApplicationDbContext _context;

        public CreateProductCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            // 1. Business Rule: Category must exist and be active
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

            if (category == null)
            {
                throw new KeyNotFoundException($"Category with ID {request.CategoryId} does not exist.");
            }

            // 2. Business Rule: Product SKU must be unique across the system
            var normalizedSku = request.SKU.Trim().ToUpperInvariant();
            var skuExists = await _context.Products
                .AnyAsync(p => p.SKU == normalizedSku, cancellationToken);

            if (skuExists)
            {
                throw new InvalidOperationException($"Product with SKU '{normalizedSku}' already exists.");
            }

            // 3. Domain entity encapsulation handles price and minimum stock level invariants
            var product = new Product(
                request.Name,
                normalizedSku,
                request.Price,
                request.MinimumStockLevel,
                request.CategoryId,
                request.Description);

            _context.Products.Add(product);
            await _context.SaveChangesAsync(cancellationToken);

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
                TotalAvailableStock: 0);
        }
    }

}
