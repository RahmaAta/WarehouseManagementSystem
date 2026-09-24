using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Products.DTOs;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Features.Products.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    string SKU,
    decimal Price,
    int MinimumStockLevel,
    int CategoryId,
    string? Description = null
) : IRequest<ProductDto>;
