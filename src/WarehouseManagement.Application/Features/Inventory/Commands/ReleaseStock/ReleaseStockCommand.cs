using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Inventory.DTOs;

namespace WarehouseManagement.Application.Features.Inventory.Commands.ReleaseStock;

public record ReleaseStockCommand(
    int WarehouseId,
    int ProductId,
    int Quantity
) : IRequest<InventoryItemDto>;
