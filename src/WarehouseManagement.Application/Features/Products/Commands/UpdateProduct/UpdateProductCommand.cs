using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Products.DTOs;

namespace WarehouseManagement.Application.Features.Products.Commands.UpdateProduct;

public record UpdateProductCommand(
    int Id,
    string Name,
    string SKU,
    decimal Price,
    int MinimumStockLevel,
    int CategoryId,
    string? Description
) : IRequest<ProductDto>;
