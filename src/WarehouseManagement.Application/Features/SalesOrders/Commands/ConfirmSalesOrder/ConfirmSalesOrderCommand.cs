using MediatR;
using WarehouseManagement.Application.Features.SalesOrders.DTOs;

namespace WarehouseManagement.Application.Features.SalesOrders.Commands.ConfirmSalesOrder;

public record ConfirmSalesOrderCommand(int Id) : IRequest<SalesOrderDto>;
