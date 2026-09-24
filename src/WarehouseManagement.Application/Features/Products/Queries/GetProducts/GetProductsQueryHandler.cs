using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.Products.DTOs;

namespace WarehouseManagement.Application.Features.Products.Queries.GetProducts
{
    public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PaginatedList<ProductDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetProductsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
            var pageSize = request.PageSize <= 0 ? 10 : (request.PageSize > 100 ? 100 : request.PageSize);

            var query = _context.Products
                .AsNoTracking();

            if (request.OnlyActive.HasValue && request.OnlyActive.Value)
            {
                query = query.Where(p => p.IsActive);
            }

            if (request.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == request.CategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                query = query.Where(p => p.Name.Contains(term) || p.SKU.Contains(term));
            }

            var projected = query
                .OrderBy(p => p.Name)
                .Select(p => new ProductDto(
                    p.Id,
                    p.Name,
                    p.SKU,
                    p.Description,
                    p.Price,
                    p.MinimumStockLevel,
                    p.CategoryId,
                    p.Category != null ? p.Category.Name : string.Empty,
                    p.IsActive,
                    p.CreatedAt,
                    p.LastModifiedAt,
                    p.InventoryItems.Sum(i => (int?)(i.Quantity - i.ReservedQuantity)) ?? 0
                ));

            return await PaginatedList<ProductDto>.CreateAsync(projected, pageNumber, pageSize, cancellationToken);
        }
    }

}
