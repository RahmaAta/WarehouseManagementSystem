namespace WarehouseManagement.Application.Features.Reports.DTOs;

/// <summary>
/// Top-selling products report: products ranked by total quantity shipped
/// (via Completed Sales Orders) in a given date range.
/// </summary>
public sealed record TopSellingProductsReportDto(
    DateTimeOffset From,
    DateTimeOffset To,
    int TopN,
    IReadOnlyList<TopSellingProductLineDto> Products,
    DateTimeOffset GeneratedAt
);

/// <summary>A single product entry in the top-selling products report.</summary>
public sealed record TopSellingProductLineDto(
    int Rank,
    int ProductId,
    string ProductName,
    string SKU,
    string CategoryName,
    int TotalQuantitySold,
    decimal TotalRevenue,
    decimal AverageUnitPrice,
    int OrderCount
);
