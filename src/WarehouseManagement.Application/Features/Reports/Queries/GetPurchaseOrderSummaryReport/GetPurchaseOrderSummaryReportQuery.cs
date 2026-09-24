using MediatR;
using WarehouseManagement.Application.Features.Reports.DTOs;

namespace WarehouseManagement.Application.Features.Reports.Queries.GetPurchaseOrderSummaryReport;

/// <summary>
/// Returns aggregated purchase order metrics (spend, status counts, top suppliers)
/// for a given date range. Defaults to the last 30 days when omitted.
/// TopSuppliersCount — number of top suppliers to include (default 10, max 50).
/// </summary>
public sealed record GetPurchaseOrderSummaryReportQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int TopSuppliersCount = 10
) : IRequest<PurchaseOrderSummaryReportDto>;
