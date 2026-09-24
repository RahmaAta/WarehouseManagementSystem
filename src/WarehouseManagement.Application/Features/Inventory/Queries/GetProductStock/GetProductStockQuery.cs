using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Inventory.DTOs;

namespace WarehouseManagement.Application.Features.Inventory.Queries.GetProductStock;

public record GetProductStockQuery(int ProductId) : IRequest<List<InventoryItemDto>>;
