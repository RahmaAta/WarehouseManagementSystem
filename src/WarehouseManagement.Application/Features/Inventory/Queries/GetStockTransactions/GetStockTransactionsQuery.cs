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
