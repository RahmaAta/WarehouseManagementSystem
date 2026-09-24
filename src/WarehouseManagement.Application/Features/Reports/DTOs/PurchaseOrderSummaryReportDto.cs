namespace WarehouseManagement.Application.Features.Reports.DTOs;

/// <summary>
/// Purchase order summary report: procurement spend broken down by supplier
/// and by order status within the queried date range.
/// </summary>
public sealed record PurchaseOrderSummaryReportDto(
    DateTimeOffset From,
    DateTimeOffset To,
    int TotalOrders,
    int DraftOrders,
    int PendingApprovalOrders,
    int ApprovedOrders,
    int ReceivedOrders,
    int CancelledOrders,
    decimal TotalSpend,
    decimal ReceivedSpend,
    decimal AverageOrderValue,
    IReadOnlyList<TopSupplierDto> TopSuppliers,
    DateTimeOffset GeneratedAt
);

/// <summary>Represents one supplier ranked by total procurement spend in the report period.</summary>
public sealed record TopSupplierDto(
    int SupplierId,
    string SupplierName,
    int OrderCount,
    decimal TotalSpend
);
