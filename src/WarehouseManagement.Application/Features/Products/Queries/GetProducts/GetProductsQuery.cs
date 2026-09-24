using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.Products.DTOs;

namespace WarehouseManagement.Application.Features.Products.Queries.GetProducts;

public record GetProductsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    int? CategoryId = null,
    string? SearchTerm = null,
    bool? OnlyActive = true
) : IRequest<PaginatedList<ProductDto>>;
