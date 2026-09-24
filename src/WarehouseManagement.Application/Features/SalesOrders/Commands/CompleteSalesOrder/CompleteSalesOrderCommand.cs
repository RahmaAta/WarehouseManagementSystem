using MediatR;
using WarehouseManagement.Application.Features.SalesOrders.DTOs;

namespace WarehouseManagement.Application.Features.SalesOrders.Commands.CompleteSalesOrder;

public record CompleteSalesOrderCommand(int Id) : IRequest<SalesOrderDto>;
