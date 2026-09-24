using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;

namespace WarehouseManagement.Application.Features.Warehouses.Commands.ActivateWarehouse;

public record ActivateWarehouseCommand(int Id) : IRequest<bool>;
