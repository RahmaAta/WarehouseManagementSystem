using System.Security.Claims;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.API.Filters;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Domain.Enums;
using WarehouseManagement.Infrastructure.Jobs;
using WarehouseManagement.UnitTests.Common;
using WarehouseManagement.UnitTests.Common.Behaviors;

namespace WarehouseManagement.UnitTests.Jobs;

public class HangfireJobsTests
{
    private readonly TestLogger<LowStockNotifierJob> _lowStockLogger = new();
    private readonly TestLogger<StaleOrderCleanupJob> _staleOrderLogger = new();
    private readonly TestLogger<DailyInventorySnapshotJob> _snapshotLogger = new();

    [Fact]
    public async Task LowStockNotifierJob_ShouldIdentifyProductsBelowSafetyThreshold()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var category = new Category("Hardware");
        context.Categories.Add(category);

        // Product A: Min level 10, total 8, reserved 0 -> Available 8 (Below threshold)
        var productA = new Product("Bolt M8", "BLT-M8-001", 5.0m, minimumStockLevel: 10, category.Id);

        // Product B: Min level 5, total 20, reserved 5 -> Available 15 (Sufficient)
        var productB = new Product("Nut M8", "NUT-M8-002", 2.0m, minimumStockLevel: 5, category.Id);

        context.Products.AddRange(productA, productB);

        var warehouse = new Warehouse("Main Warehouse", "Building 1");
        context.Warehouses.Add(warehouse);

        var itemA = new InventoryItem(warehouse.Id, productA.Id, initialQuantity: 8);
        var itemB = new InventoryItem(warehouse.Id, productB.Id, initialQuantity: 20);
        itemB.ReserveStock(5); // 15 available

        context.InventoryItems.AddRange(itemA, itemB);
        await context.SaveChangesAsync();

        var job = new LowStockNotifierJob(context, _lowStockLogger);

        // Act
        var lowStockCount = await job.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, lowStockCount); // Only Product A is below threshold
        Assert.Contains(_lowStockLogger.Logs, l => l.Message.Contains("LOW STOCK ALERT") && l.Message.Contains("Bolt M8"));
    }

    [Fact]
    public async Task StaleOrderCleanupJob_ShouldCancelExpiredPendingAndConfirmedOrdersAndReleaseReservedStock()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var customer = new Customer("Global Logistics", "contact@globallogistics.com", "12345678");
        var warehouse = new Warehouse("Hub 1", "Zone A");
        var category = new Category("Raw Materials");
        context.Categories.Add(category);
        var product = new Product("Steel Rod", "STL-ROD-01", 100m, 5, category.Id);

        context.Customers.Add(customer);
        context.Warehouses.Add(warehouse);
        context.Products.Add(product);

        // 100 units initial, 20 reserved for stale order
        var inventoryItem = new InventoryItem(warehouse.Id, product.Id, initialQuantity: 100);
        inventoryItem.ReserveStock(20);
        context.InventoryItems.Add(inventoryItem);

        var staleConfirmedOrder = new SalesOrder("SO-STALE-CONFIRMED", customer.Id, warehouse.Id);
        staleConfirmedOrder.AddItem(product.Id, 20, 100m);
        staleConfirmedOrder.Confirm("Manager");

        // Fresh Pending order created now (should NOT be cancelled)
        var freshOrder = new SalesOrder("SO-FRESH", customer.Id, warehouse.Id);
        freshOrder.AddItem(product.Id, 5, 100m);

        context.SalesOrders.AddRange(staleConfirmedOrder, freshOrder);
        await context.SaveChangesAsync();

        // Backdate staleConfirmedOrder CreatedAt to 50 hours ago
        context.Entry(staleConfirmedOrder).Property(x => x.CreatedAt).CurrentValue = DateTime.UtcNow.AddHours(-50);
        await context.SaveChangesAsync();

        var job = new StaleOrderCleanupJob(context, _staleOrderLogger);

        // Act
        var cancelledCount = await job.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, cancelledCount);

        var updatedOrder = await context.SalesOrders.FirstOrDefaultAsync(so => so.Id == staleConfirmedOrder.Id);
        Assert.NotNull(updatedOrder);
        Assert.Equal(OrderStatus.Cancelled, updatedOrder.Status);

        // Verify reserved stock was released back to 0, available back to 100
        var updatedItem = await context.InventoryItems.FirstOrDefaultAsync(i => i.Id == inventoryItem.Id);
        Assert.NotNull(updatedItem);
        Assert.Equal(0, updatedItem.ReservedQuantity);
        Assert.Equal(100, updatedItem.AvailableQuantity);

        // Verify fresh order was left in Pending status
        var updatedFreshOrder = await context.SalesOrders.FirstOrDefaultAsync(so => so.Id == freshOrder.Id);
        Assert.NotNull(updatedFreshOrder);
        Assert.Equal(OrderStatus.Pending, updatedFreshOrder.Status);
    }

    [Fact]
    public async Task DailyInventorySnapshotJob_ShouldCalculateTotalsAndLogAuditMetrics()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var category = new Category("Chemicals");
        context.Categories.Add(category);
        var product = new Product("Solvent X", "SLV-X-01", 50.0m, 10, category.Id);
        var warehouse = new Warehouse("Chemical Depot", "Sector 7");
        context.Products.Add(product);
        context.Warehouses.Add(warehouse);

        var item = new InventoryItem(warehouse.Id, product.Id, initialQuantity: 200);
        item.ReserveStock(30);
        context.InventoryItems.Add(item);
        await context.SaveChangesAsync();

        var job = new DailyInventorySnapshotJob(context, _snapshotLogger);

        // Act
        var recordCount = await job.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, recordCount);
        Assert.Contains(_snapshotLogger.Logs, l => l.Message.Contains("DAILY INVENTORY VALUATION & AUDIT SNAPSHOT"));
        Assert.Contains(_snapshotLogger.Logs, l => l.Message.Contains("$10,000.00")); // 200 units * $50 = $10,000
    }

    [Fact]
    public void HangfireAuthorizationFilter_WhenLocalhostRequest_ShouldAuthorize()
    {
        // Arrange
        var filter = new HangfireAuthorizationFilter();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("localhost", 5000);

        // Act
        var authorized = filter.Authorize(httpContext);

        // Assert
        Assert.True(authorized);
    }

    [Fact]
    public void HangfireAuthorizationFilter_WhenRemoteNonAdmin_ShouldDeny()
    {
        // Arrange
        var filter = new HangfireAuthorizationFilter();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("warehouse-wms.company.com");
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "employee"),
            new Claim(ClaimTypes.Role, "WarehouseStaff") // Not Admin!
        }, "Bearer"));

        // Act
        var authorized = filter.Authorize(httpContext);

        // Assert
        Assert.False(authorized);
    }

    [Fact]
    public void HangfireAuthorizationFilter_WhenRemoteAdmin_ShouldAuthorize()
    {
        // Arrange
        var filter = new HangfireAuthorizationFilter();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("warehouse-wms.company.com");
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "superadmin"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Bearer"));

        // Act
        var authorized = filter.Authorize(httpContext);

        // Assert
        Assert.True(authorized);
    }
}
