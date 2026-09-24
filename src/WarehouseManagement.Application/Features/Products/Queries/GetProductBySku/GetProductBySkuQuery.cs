using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Products.DTOs;

namespace WarehouseManagement.Application.Features.Products.Queries.GetProductBySku;

public record GetProductBySkuQuery(string Sku) : IRequest<ProductDto>;
