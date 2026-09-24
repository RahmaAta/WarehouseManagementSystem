using MediatR;
using WarehouseManagement.Application.Features.SalesOrders.DTOs;

namespace WarehouseManagement.Application.Features.SalesOrders.Commands.CancelSalesOrder;

public record CancelSalesOrderCommand(int Id) : IRequest<SalesOrderDto>;
