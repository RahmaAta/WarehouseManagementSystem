using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Features.Reports.DTOs;
using WarehouseManagement.Application.Features.Reports.Queries.GetInventoryValuationReport;
using WarehouseManagement.Application.Features.Reports.Queries.GetPurchaseOrderSummaryReport;
using WarehouseManagement.Application.Features.Reports.Queries.GetSalesOrderSummaryReport;
using WarehouseManagement.Application.Features.Reports.Queries.GetTopSellingProductsReport;
using WarehouseManagement.Application.Features.Reports.Queries.GetWarehouseUtilizationReport;

namespace WarehouseManagement.API.Controllers;

/// <summary>
/// Reporting &amp; Analytics endpoints.
/// All endpoints require authentication; Admin and WarehouseManager roles can access them.
/// Reports are read-only aggregations — no data mutation occurs here.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,WarehouseManager")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ──────────────────────────────────────────────────────────
    // GET api/reports/inventory-valuation
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a per-warehouse inventory valuation report.
    /// Computes TotalStockValue = SUM(Quantity * Product.Price) per warehouse.
    /// </summary>
    /// <param name="warehouseId">Optional — restrict report to a single warehouse.</param>
    /// <param name="includeInactive">Include inactive warehouses (default false).</param>
    [HttpGet("inventory-valuation")]
    [ProducesResponseType(typeof(IReadOnlyList<InventoryValuationReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<InventoryValuationReportDto>>> GetInventoryValuation(
        [FromQuery] int? warehouseId = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetInventoryValuationReportQuery(warehouseId, includeInactive),
            cancellationToken);

        return Ok(result);
    }

    // ──────────────────────────────────────────────────────────
    // GET api/reports/sales-orders
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns an aggregated sales order report: revenue, order counts by status,
    /// and top customers by spend for a given date range (defaults to last 30 days).
    /// </summary>
    /// <param name="from">Start of the date range (UTC).</param>
    /// <param name="to">End of the date range (UTC). Cannot be in the future.</param>
    /// <param name="topCustomersCount">Number of top customers to include (1–50, default 10).</param>
    [HttpGet("sales-orders")]
    [ProducesResponseType(typeof(SalesOrderSummaryReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SalesOrderSummaryReportDto>> GetSalesOrderSummary(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int topCustomersCount = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetSalesOrderSummaryReportQuery(from, to, topCustomersCount),
            cancellationToken);

        return Ok(result);
    }

    // ──────────────────────────────────────────────────────────
    // GET api/reports/purchase-orders
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns an aggregated purchase order report: procurement spend, order counts
    /// by status, and top suppliers by spend for a given date range.
    /// </summary>
    /// <param name="from">Start of the date range (UTC).</param>
    /// <param name="to">End of the date range (UTC). Cannot be in the future.</param>
    /// <param name="topSuppliersCount">Number of top suppliers to include (1–50, default 10).</param>
    [HttpGet("purchase-orders")]
    [ProducesResponseType(typeof(PurchaseOrderSummaryReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PurchaseOrderSummaryReportDto>> GetPurchaseOrderSummary(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int topSuppliersCount = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetPurchaseOrderSummaryReportQuery(from, to, topSuppliersCount),
            cancellationToken);

        return Ok(result);
    }

    // ──────────────────────────────────────────────────────────
    // GET api/reports/top-selling-products
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the top N products ranked by units sold through Completed Sales Orders.
    /// Optionally filter by category.
    /// </summary>
    /// <param name="from">Start of the date range (UTC).</param>
    /// <param name="to">End of the date range (UTC).</param>
    /// <param name="topN">Number of products to return (1–100, default 10).</param>
    /// <param name="categoryId">Optional — restrict to a single product category.</param>
    [HttpGet("top-selling-products")]
    [ProducesResponseType(typeof(TopSellingProductsReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TopSellingProductsReportDto>> GetTopSellingProducts(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int topN = 10,
        [FromQuery] int? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetTopSellingProductsReportQuery(from, to, topN, categoryId),
            cancellationToken);

        return Ok(result);
    }

    // ──────────────────────────────────────────────────────────
    // GET api/reports/warehouse-utilization
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a utilization snapshot per active warehouse: total units, reserved units,
    /// available units, low-stock and out-of-stock product counts, and stock value.
    /// Optionally includes per-product drill-down.
    /// </summary>
    /// <param name="warehouseId">Optional — restrict to a single warehouse.</param>
    /// <param name="includeProductDetails">Include per-product breakdown (default true).</param>
    [HttpGet("warehouse-utilization")]
    [ProducesResponseType(typeof(WarehouseUtilizationReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WarehouseUtilizationReportDto>> GetWarehouseUtilization(
        [FromQuery] int? warehouseId = null,
        [FromQuery] bool includeProductDetails = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetWarehouseUtilizationReportQuery(warehouseId, includeProductDetails),
            cancellationToken);

        return Ok(result);
    }
}
