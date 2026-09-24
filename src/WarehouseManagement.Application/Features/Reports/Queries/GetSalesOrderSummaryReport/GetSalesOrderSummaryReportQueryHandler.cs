using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Reports.DTOs;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Application.Features.Reports.Queries.GetSalesOrderSummaryReport;

/// <summary>
/// Aggregates sales order metrics (revenue, status counts, top customers) for a date range.
///
/// Design choices:
/// - Two separate queries (aggregate + top-customers) rather than one huge GroupBy JOIN
///   because EF Core's GroupBy translation has limits with multi-level navigation joins.
/// - Status filtering uses the domain enum OrderStatus (not SalesOrderStatus —
///   the domain enum for SalesOrder status is called OrderStatus).
/// - TopCustomersCount is clamped to [1, 50] defensively; the validator also enforces this.
/// - Both queries use AsNoTracking() — no state mutation occurs in a report.
/// </summary>
public class GetSalesOrderSummaryReportQueryHandler
    : IRequestHandler<GetSalesOrderSummaryReportQuery, SalesOrderSummaryReportDto>
{
    private readonly IApplicationDbContext _context;

    public GetSalesOrderSummaryReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SalesOrderSummaryReportDto> Handle(
        GetSalesOrderSummaryReportQuery request,
        CancellationToken cancellationToken)
    {
        var generatedAt = DateTimeOffset.UtcNow;
        var from = request.From ?? generatedAt.AddDays(-30);
        var to   = request.To   ?? generatedAt;
        var topN = Math.Clamp(request.TopCustomersCount, 1, 50);

        // Materialize all orders in range (bounded set) — avoids GroupBy translation issues
        var orders = await _context.SalesOrders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= from.UtcDateTime && o.CreatedAt <= to.UtcDateTime)
            .ToListAsync(cancellationToken);

        // Compute aggregates client-side
        int total       = orders.Count;
        int pending     = orders.Count(o => o.Status == OrderStatus.Pending);
        int confirmed   = orders.Count(o => o.Status == OrderStatus.Confirmed);
        int completed   = orders.Count(o => o.Status == OrderStatus.Completed);
        int cancelled   = orders.Count(o => o.Status == OrderStatus.Cancelled);
        decimal revenue         = orders.Sum(o => o.TotalAmount);
        decimal completedRevenue = orders
            .Where(o => o.Status == OrderStatus.Completed)
            .Sum(o => o.TotalAmount);

        // Top customers — from orders not cancelled, join with Customer nav-prop
        // Load customers for navigation
        var customerIds = orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .Select(o => o.CustomerId)
            .Distinct()
            .ToList();

        var customerNames = await _context.Customers
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var topCustomers = orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.CustomerId)
            .Select(g => new TopCustomerDto(
                CustomerId:   g.Key,
                CustomerName: customerNames.GetValueOrDefault(g.Key, string.Empty),
                OrderCount:   g.Count(),
                TotalSpend:   g.Sum(o => o.TotalAmount)
            ))
            .OrderByDescending(c => c.TotalSpend)
            .Take(topN)
            .ToList()
            .AsReadOnly();

        return new SalesOrderSummaryReportDto(
            From:             from,
            To:               to,
            TotalOrders:      total,
            PendingOrders:    pending,
            ConfirmedOrders:  confirmed,
            CompletedOrders:  completed,
            CancelledOrders:  cancelled,
            TotalRevenue:     revenue,
            CompletedRevenue: completedRevenue,
            AverageOrderValue: total > 0 ? revenue / total : 0m,
            TopCustomers:     topCustomers,
            GeneratedAt:      generatedAt
        );
    }
}
