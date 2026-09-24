using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Categories.DTOs;

namespace WarehouseManagement.Application.Features.Categories.Queries.GetCategories
{
    public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetCategoriesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Categories.AsNoTracking();

            if (!request.IncludeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            return await query
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto(
                    c.Id,
                    c.Name,
                    c.Description,
                    c.IsActive,
                    c.Products.Count))
                .ToListAsync(cancellationToken);
        }
    }

}
