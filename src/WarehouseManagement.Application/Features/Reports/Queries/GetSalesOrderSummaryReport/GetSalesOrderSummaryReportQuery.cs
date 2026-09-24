using MediatR;
using WarehouseManagement.Application.Features.Reports.DTOs;

namespace WarehouseManagement.Application.Features.Reports.Queries.GetSalesOrderSummaryReport;

/// <summary>
/// Returns aggregated sales order metrics (revenue, counts by status, top customers)
/// for a given date range. Defaults to the last 30 days when omitted.
/// TopCustomersCount — number of top customers to include (default 10, max 50).
/// </summary>
public sealed record GetSalesOrderSummaryReportQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int TopCustomersCount = 10
) : IRequest<SalesOrderSummaryReportDto>;
