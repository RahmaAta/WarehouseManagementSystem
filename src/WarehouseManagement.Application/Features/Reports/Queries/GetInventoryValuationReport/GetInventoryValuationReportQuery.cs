using MediatR;
using WarehouseManagement.Application.Features.Reports.DTOs;

namespace WarehouseManagement.Application.Features.Reports.Queries.GetInventoryValuationReport;

/// <summary>
/// Returns a per-warehouse inventory valuation report.
/// WarehouseId — optional filter to limit report to a single warehouse.
/// IncludeInactive — when true, inactive warehouses are included.
/// </summary>
public sealed record GetInventoryValuationReportQuery(
    int? WarehouseId = null,
    bool IncludeInactive = false
) : IRequest<IReadOnlyList<InventoryValuationReportDto>>;
