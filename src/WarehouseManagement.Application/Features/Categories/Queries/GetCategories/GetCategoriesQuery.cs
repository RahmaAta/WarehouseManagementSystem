using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Categories.DTOs;

namespace WarehouseManagement.Application.Features.Categories.Queries.GetCategories;

public record GetCategoriesQuery(bool IncludeInactive = false) : IRequest<List<CategoryDto>>;