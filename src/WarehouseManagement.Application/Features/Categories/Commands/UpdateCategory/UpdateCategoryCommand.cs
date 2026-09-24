using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Categories.DTOs;

namespace WarehouseManagement.Application.Features.Categories.Commands.UpdateCategory;

public record UpdateCategoryCommand(
    int Id,
    string Name,
    string? Description
) : IRequest<CategoryDto>;