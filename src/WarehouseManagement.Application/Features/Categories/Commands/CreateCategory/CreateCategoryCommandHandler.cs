using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Categories.DTOs;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Features.Categories.Commands.CreateCategory
{
    public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
    {
        private readonly IApplicationDbContext _context;

        public CreateCategoryCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            var trimmedName = request.Name.Trim();

            var exists = await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException($"Category with name '{request.Name}' already exists.");
            }

            var category = new Category(trimmedName, request.Description?.Trim());

            _context.Categories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            return new CategoryDto(
                category.Id,
                category.Name,
                category.Description,
                category.IsActive,
                ProductsCount: 0);
        }
    }
}
