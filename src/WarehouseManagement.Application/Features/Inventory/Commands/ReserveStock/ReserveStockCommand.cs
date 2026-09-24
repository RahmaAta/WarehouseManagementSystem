using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Inventory.DTOs;

namespace WarehouseManagement.Application.Features.Inventory.Commands.ReserveStock;

public record ReserveStockCommand(
    int WarehouseId,
    int ProductId,
    int Quantity
) : IRequest<InventoryItemDto>;
