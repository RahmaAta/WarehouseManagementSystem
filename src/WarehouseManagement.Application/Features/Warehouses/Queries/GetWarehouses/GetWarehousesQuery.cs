using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Warehouses.DTOs;

namespace WarehouseManagement.Application.Features.Warehouses.Queries.GetWarehouses;

public record GetWarehousesQuery(bool IncludeInactive = false) : IRequest<List<WarehouseDto>>;
