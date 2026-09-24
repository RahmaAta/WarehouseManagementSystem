using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.SalesOrders.DTOs;

namespace WarehouseManagement.Application.Features.SalesOrders.Queries.GetSalesOrderById;

public class GetSalesOrderByIdQueryHandler : IRequestHandler<GetSalesOrderByIdQuery, SalesOrderDto>
{
    private readonly IApplicationDbContext _context;

    public GetSalesOrderByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SalesOrderDto> Handle(GetSalesOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var salesOrder = await _context.SalesOrders
            .AsNoTracking()
            .Include(so => so.Customer)
            .Include(so => so.Warehouse)
            .Include(so => so.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(so => so.Id == request.Id, cancellationToken);

        if (salesOrder == null)
        {
            throw new KeyNotFoundException($"Sales order with ID {request.Id} was not found.");
        }

        var itemDtos = salesOrder.Items.Select(i => new SalesOrderItemDto(
            i.Id,
            i.ProductId,
            i.Product?.Name ?? string.Empty,
            i.Product?.SKU ?? string.Empty,
            i.Quantity,
            i.UnitPrice,
            i.TotalPrice
        )).ToList();

        return new SalesOrderDto(
            salesOrder.Id,
            salesOrder.OrderNumber,
            salesOrder.CustomerId,
            salesOrder.Customer?.Name ?? string.Empty,
            salesOrder.WarehouseId,
            salesOrder.Warehouse?.Name ?? string.Empty,
            salesOrder.Status.ToString(),
            salesOrder.TotalAmount,
            salesOrder.ConfirmedBy,
            salesOrder.ConfirmedAt,
            salesOrder.CompletedBy,
            salesOrder.CompletedAt,
            salesOrder.CreatedAt,
            itemDtos
        );
    }
}
