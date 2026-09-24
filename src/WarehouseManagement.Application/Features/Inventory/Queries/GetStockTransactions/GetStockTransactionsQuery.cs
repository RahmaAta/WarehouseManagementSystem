using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.Inventory.DTOs;

namespace WarehouseManagement.Application.Features.Inventory.Queries.GetStockTransactions;

public record GetStockTransactionsQuery(
    int? ProductId = null,
    int? WarehouseId = null,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PaginatedList<StockTransactionDto>>;

public class GetStockTransactionsQueryHandler : IRequestHandler<GetStockTransactionsQuery, PaginatedList<StockTransactionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetStockTransactionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<StockTransactionDto>> Handle(GetStockTransactionsQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 20 : (request.PageSize > 100 ? 100 : request.PageSize);

        var query = _context.StockTransactions
            .AsNoTracking();

        if (request.ProductId.HasValue)
        {
            query = query.Where(t => t.ProductId == request.ProductId.Value);
        }

        if (request.WarehouseId.HasValue)
        {
            query = query.Where(t => t.WarehouseId == request.WarehouseId.Value || t.ToWarehouseId == request.WarehouseId.Value);
        }

        var projected = query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new StockTransactionDto(
                t.Id,
                t.ProductId,
                t.Product != null ? t.Product.Name : string.Empty,
                t.Product != null ? t.Product.SKU : string.Empty,
                t.WarehouseId,
                t.Warehouse != null ? t.Warehouse.Name : string.Empty,
                t.ToWarehouseId,
                t.ToWarehouse != null ? t.ToWarehouse.Name : null,
                t.Type.ToString(),
                t.Quantity,
                t.ReferenceId,
                t.Notes,
                t.CreatedAt,
                t.CreatedBy
            ));

        return await PaginatedList<StockTransactionDto>.CreateAsync(projected, pageNumber, pageSize, cancellationToken);
    }
}
