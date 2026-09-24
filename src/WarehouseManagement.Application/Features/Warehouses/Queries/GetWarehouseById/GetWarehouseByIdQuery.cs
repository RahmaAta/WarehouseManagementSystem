using MediatR;
using WarehouseManagement.Application.Features.Warehouses.DTOs;

namespace WarehouseManagement.Application.Features.Warehouses.Queries.GetWarehouseById;

public record GetWarehouseByIdQuery(int Id) : IRequest<WarehouseDto>;
