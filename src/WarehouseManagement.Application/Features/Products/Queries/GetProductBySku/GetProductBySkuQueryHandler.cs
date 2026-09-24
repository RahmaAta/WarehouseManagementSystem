using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Products.DTOs;

namespace WarehouseManagement.Application.Features.Products.Queries.GetProductBySku
{
    public class GetProductBySkuQueryHandler : IRequestHandler<GetProductBySkuQuery, ProductDto>
    {
        private readonly IApplicationDbContext _context;

        public GetProductBySkuQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ProductDto> Handle(GetProductBySkuQuery request, CancellationToken cancellationToken)
        {
            var normalizedSku = request.Sku.Trim().ToUpperInvariant();

            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.InventoryItems)
                .FirstOrDefaultAsync(p => p.SKU == normalizedSku, cancellationToken);

            if (product == null)
            {
                throw new KeyNotFoundException($"Product with SKU '{normalizedSku}' was not found.");
            }

            return new ProductDto(
                product.Id,
                product.Name,
                product.SKU,
                product.Description,
                product.Price,
                product.MinimumStockLevel,
                product.CategoryId,
                product.Category?.Name ?? string.Empty,
                product.IsActive,
                product.CreatedAt,
                product.LastModifiedAt,
                product.InventoryItems.Sum(i => i.AvailableQuantity)
            );
        }
    }

}
