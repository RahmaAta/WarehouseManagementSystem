using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Inventory.DTOs;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Features.Inventory.Commands.AdjustStock;

public record AdjustStockCommand(
    int WarehouseId,
    int ProductId,
    int ActualCountedQuantity,
    string Reason,
    string? ReferenceId = null
) : IRequest<InventoryItemDto>;
