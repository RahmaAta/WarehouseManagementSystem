using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Categories.DTOs;

namespace WarehouseManagement.Application.Features.Categories.Commands.UpdateCategory
{
    public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
    {
        private readonly IApplicationDbContext _context;

        public UpdateCategoryCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (category == null)
            {
                throw new KeyNotFoundException($"Category with ID {request.Id} was not found.");
            }

            var trimmedName = request.Name.Trim();

            var duplicateExists = await _context.Categories
                .AnyAsync(c => c.Id != request.Id && c.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

            if (duplicateExists)
            {
                throw new InvalidOperationException($"Another category with name '{request.Name}' already exists.");
            }

            category.Update(trimmedName, request.Description?.Trim());

            await _context.SaveChangesAsync(cancellationToken);

            return new CategoryDto(
                category.Id,
                category.Name,
                category.Description,
                category.IsActive,
                category.Products.Count);
        }
    }

}
