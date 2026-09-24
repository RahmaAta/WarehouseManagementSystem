using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Inventory.DTOs;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Features.Inventory.Commands.AddStock;

public record AddStockCommand(
    int WarehouseId,
    int ProductId,
    int Quantity,
    string? ReferenceId = null,
    string? Notes = null
) : IRequest<InventoryItemDto>;
