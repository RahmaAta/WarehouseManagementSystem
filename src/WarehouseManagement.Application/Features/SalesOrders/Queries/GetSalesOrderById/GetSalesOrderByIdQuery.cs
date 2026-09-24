using MediatR;
using WarehouseManagement.Application.Features.SalesOrders.DTOs;

namespace WarehouseManagement.Application.Features.SalesOrders.Queries.GetSalesOrderById;

public record GetSalesOrderByIdQuery(int Id) : IRequest<SalesOrderDto>;
