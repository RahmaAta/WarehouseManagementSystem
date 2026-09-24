using MediatR;
using WarehouseManagement.Application.Features.Warehouses.DTOs;

namespace WarehouseManagement.Application.Features.Warehouses.Commands.UpdateWarehouse;

public record UpdateWarehouseCommand(int Id, string Name, string Location) : IRequest<WarehouseDto>;
