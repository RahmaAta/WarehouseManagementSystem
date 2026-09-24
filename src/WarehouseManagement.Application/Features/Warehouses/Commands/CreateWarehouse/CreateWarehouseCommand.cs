using MediatR;
using WarehouseManagement.Application.Features.Warehouses.DTOs;

namespace WarehouseManagement.Application.Features.Warehouses.Commands.CreateWarehouse;

public record CreateWarehouseCommand(string Name, string Location) : IRequest<WarehouseDto>;
