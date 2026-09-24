using MediatR;
using WarehouseManagement.Application.Features.Reports.DTOs;

namespace WarehouseManagement.Application.Features.Reports.Queries.GetWarehouseUtilizationReport;

/// <summary>
/// Returns a full utilization snapshot of all (or one) active warehouse(s).
/// WarehouseId — optional filter to limit to a single warehouse.
/// IncludeProductDetails — when true, each warehouse entry includes per-product
///   stock breakdown; set false for a lightweight summary-only response.
/// </summary>
public sealed record GetWarehouseUtilizationReportQuery(
    int? WarehouseId = null,
    bool IncludeProductDetails = true
) : IRequest<WarehouseUtilizationReportDto>;
