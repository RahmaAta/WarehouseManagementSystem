using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.Reports.DTOs;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Application.Features.Reports.Queries.GetPurchaseOrderSummaryReport;

/// <summary>
/// Aggregates purchase order metrics (spend by status, top suppliers).
/// Mirrors the SalesOrderSummaryReport pattern: materialise a bounded date-range
/// set then aggregate client-side to avoid EF GroupBy translation issues.
/// </summary>
public class GetPurchaseOrderSummaryReportQueryHandler
    : IRequestHandler<GetPurchaseOrderSummaryReportQuery, PurchaseOrderSummaryReportDto>
{
    private readonly IApplicationDbContext _context;

    public GetPurchaseOrderSummaryReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PurchaseOrderSummaryReportDto> Handle(
        GetPurchaseOrderSummaryReportQuery request,
        CancellationToken cancellationToken)
    {
        var generatedAt = DateTimeOffset.UtcNow;
        var from = request.From ?? generatedAt.AddDays(-30);
        var to   = request.To   ?? generatedAt;
        var topN = Math.Clamp(request.TopSuppliersCount, 1, 50);

        // Materialise bounded order set
        var orders = await _context.PurchaseOrders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= from.UtcDateTime && o.CreatedAt <= to.UtcDateTime)
            .ToListAsync(cancellationToken);

        int total            = orders.Count;
        int draft            = orders.Count(o => o.Status == PurchaseOrderStatus.Draft);
        int pendingApproval  = orders.Count(o => o.Status == PurchaseOrderStatus.PendingApproval);
        int approved         = orders.Count(o => o.Status == PurchaseOrderStatus.Approved);
        int received         = orders.Count(o => o.Status == PurchaseOrderStatus.Received);
        int cancelled        = orders.Count(o => o.Status == PurchaseOrderStatus.Cancelled);
        decimal totalSpend   = orders.Sum(o => o.TotalAmount);
        decimal receivedSpend = orders
            .Where(o => o.Status == PurchaseOrderStatus.Received)
            .Sum(o => o.TotalAmount);

        // Load supplier names for top-N calculation
        var supplierIds = orders
            .Where(o => o.Status != PurchaseOrderStatus.Cancelled)
            .Select(o => o.SupplierId)
            .Distinct()
            .ToList();

        var supplierNames = await _context.Suppliers
            .AsNoTracking()
            .Where(s => supplierIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name })
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var topSuppliers = orders
            .Where(o => o.Status != PurchaseOrderStatus.Cancelled)
            .GroupBy(o => o.SupplierId)
            .Select(g => new TopSupplierDto(
                SupplierId:   g.Key,
                SupplierName: supplierNames.GetValueOrDefault(g.Key, string.Empty),
                OrderCount:   g.Count(),
                TotalSpend:   g.Sum(o => o.TotalAmount)
            ))
            .OrderByDescending(s => s.TotalSpend)
            .Take(topN)
            .ToList()
            .AsReadOnly();

        return new PurchaseOrderSummaryReportDto(
            From:                  from,
            To:                    to,
            TotalOrders:           total,
            DraftOrders:           draft,
            PendingApprovalOrders: pendingApproval,
            ApprovedOrders:        approved,
            ReceivedOrders:        received,
            CancelledOrders:       cancelled,
            TotalSpend:            totalSpend,
            ReceivedSpend:         receivedSpend,
            AverageOrderValue:     total > 0 ? totalSpend / total : 0m,
            TopSuppliers:          topSuppliers,
            GeneratedAt:           generatedAt
        );
    }
}
