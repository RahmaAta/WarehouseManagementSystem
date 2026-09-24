using MediatR;
using WarehouseManagement.Application.Features.Reports.DTOs;

namespace WarehouseManagement.Application.Features.Reports.Queries.GetTopSellingProductsReport;

/// <summary>
/// Returns the top N products by quantity sold via Completed Sales Orders
/// within a given date range.
/// TopN — number of products to return (default 10, max 100).
/// CategoryId — optional filter to restrict to one product category.
/// </summary>
public sealed record GetTopSellingProductsReportQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int TopN = 10,
    int? CategoryId = null
) : IRequest<TopSellingProductsReportDto>;
