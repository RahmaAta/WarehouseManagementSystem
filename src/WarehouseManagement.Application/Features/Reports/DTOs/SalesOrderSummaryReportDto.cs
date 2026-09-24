namespace WarehouseManagement.Application.Features.Reports.DTOs;

/// <summary>
/// Sales order summary report: revenue + order-count broken down by status
/// for a given date range, with optional customer filter.
/// </summary>
public sealed record SalesOrderSummaryReportDto(
    DateTimeOffset From,
    DateTimeOffset To,
    int TotalOrders,
    int PendingOrders,
    int ConfirmedOrders,
    int CompletedOrders,
    int CancelledOrders,
    decimal TotalRevenue,
    decimal CompletedRevenue,
    decimal AverageOrderValue,
    IReadOnlyList<TopCustomerDto> TopCustomers,
    DateTimeOffset GeneratedAt
);

/// <summary>Represents one customer ranked by their total spend in the report period.</summary>
public sealed record TopCustomerDto(
    int CustomerId,
    string CustomerName,
    int OrderCount,
    decimal TotalSpend
);
