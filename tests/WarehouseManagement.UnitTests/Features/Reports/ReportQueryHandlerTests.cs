using WarehouseManagement.Application.Features.Reports.Queries.GetInventoryValuationReport;
using WarehouseManagement.Application.Features.Reports.Queries.GetPurchaseOrderSummaryReport;
using WarehouseManagement.Application.Features.Reports.Queries.GetSalesOrderSummaryReport;
using WarehouseManagement.Application.Features.Reports.Queries.GetTopSellingProductsReport;
using WarehouseManagement.Application.Features.Reports.Queries.GetWarehouseUtilizationReport;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Domain.Enums;
using WarehouseManagement.UnitTests.Common;

namespace WarehouseManagement.UnitTests.Features.Reports;

/// <summary>
/// Unit tests for all five Reporting &amp; Analytics query handlers.
///
/// Test pattern:
///   - Uses TestDbContextFactory.Create() for a fresh InMemory DB per test.
///   - Entities are created via their domain constructors (which enforce
///     invariants), then persisted via SaveChangesAsync() so EF assigns
///     auto-increment int IDs before we reference them.
///   - SalesOrder.AddItem() / PurchaseOrder.AddItem() are used (private-set
///     Items collection enforces domain rules).
///
/// EF InMemory limitation:
///   BeginTransactionAsync returns NoOpTransaction for InMemory. Report
///   handlers are read-only and do not use transactions, so this is irrelevant.
/// </summary>
public class ReportQueryHandlerTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // GetInventoryValuationReportQueryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task InventoryValuation_ReturnsOneEntry_WhenSingleWarehouseWithTwoProducts()
    {
        // Arrange
        using var ctx = TestDbContextFactory.Create();

        var cat  = new Category("Electronics");
        ctx.Categories.Add(cat);
        await ctx.SaveChangesAsync();

        var p1 = new Product("Laptop",  "LAP-001", 1000m, 5, cat.Id);
        var p2 = new Product("Monitor", "MON-001", 400m,  3, cat.Id);
        ctx.Products.AddRange(p1, p2);

        var wh = new Warehouse("WH-Alpha", "Cairo");
        ctx.Warehouses.Add(wh);
        await ctx.SaveChangesAsync();

        // InventoryItem has a protected constructor — use domain constructor
        var i1 = new InventoryItem(wh.Id, p1.Id, 10);
        var i2 = new InventoryItem(wh.Id, p2.Id, 5);
        ctx.InventoryItems.AddRange(i1, i2);
        await ctx.SaveChangesAsync();

        // Reserve 3 units of p1 so we can test reserved/available split
        i1.ReserveStock(3);
        await ctx.SaveChangesAsync();

        var handler = new GetInventoryValuationReportQueryHandler(ctx);

        // Act
        var result = await handler.Handle(new GetInventoryValuationReportQuery(), CancellationToken.None);

        // Assert
        Assert.Single(result);
        var entry = result[0];
        Assert.Equal(wh.Id, entry.WarehouseId);
        Assert.Equal("WH-Alpha", entry.WarehouseName);
        Assert.Equal(2,       entry.TotalDistinctProducts);
        Assert.Equal(15,      entry.TotalUnitsOnHand);          // 10 + 5
        Assert.Equal(3,       entry.TotalUnitsReserved);         // reserved on p1
        Assert.Equal(12,      entry.TotalUnitsAvailable);        // 15 - 3
        Assert.Equal(12000m,  entry.TotalStockValue);            // 10*1000 + 5*400
        Assert.Equal(3000m,   entry.ReservedStockValue);         // 3*1000
        Assert.Equal(9000m,   entry.AvailableStockValue);        // 12000 - 3000
    }

    [Fact]
    public async Task InventoryValuation_ExcludesInactiveWarehouses_ByDefault()
    {
        // Arrange
        using var ctx = TestDbContextFactory.Create();

        var cat = new Category("Tools");
        ctx.Categories.Add(cat);

        var activeWh   = new Warehouse("Active-WH",   "Cairo");
        var inactiveWh = new Warehouse("Inactive-WH", "Alex");
        ctx.Warehouses.AddRange(activeWh, inactiveWh);
        await ctx.SaveChangesAsync();

        inactiveWh.Deactivate();

        var p = new Product("Wrench", "WRN-001", 50m, 2, cat.Id);
        ctx.Products.Add(p);
        await ctx.SaveChangesAsync();

        ctx.InventoryItems.AddRange(
            new InventoryItem(activeWh.Id,   p.Id, 10),
            new InventoryItem(inactiveWh.Id, p.Id, 20)
        );
        await ctx.SaveChangesAsync();

        var handler = new GetInventoryValuationReportQueryHandler(ctx);

        // Act — default: exclude inactive
        var result = await handler.Handle(new GetInventoryValuationReportQuery(IncludeInactive: false), CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(activeWh.Id, result[0].WarehouseId);
        Assert.Equal(10, result[0].TotalUnitsOnHand);
    }

    [Fact]
    public async Task InventoryValuation_IncludesInactiveWarehouses_WhenFlagIsTrue()
    {
        // Arrange
        using var ctx = TestDbContextFactory.Create();

        var cat = new Category("Electronics");
        ctx.Categories.Add(cat);

        var activeWh   = new Warehouse("A-Wh", "Cairo");
        var inactiveWh = new Warehouse("I-Wh", "Alex");
        ctx.Warehouses.AddRange(activeWh, inactiveWh);
        await ctx.SaveChangesAsync();

        inactiveWh.Deactivate();

        var p = new Product("Cable", "CBL-001", 10m, 1, cat.Id);
        ctx.Products.Add(p);
        await ctx.SaveChangesAsync();

        ctx.InventoryItems.AddRange(
            new InventoryItem(activeWh.Id,   p.Id, 5),
            new InventoryItem(inactiveWh.Id, p.Id, 8)
        );
        await ctx.SaveChangesAsync();

        var handler = new GetInventoryValuationReportQueryHandler(ctx);

        // Act
        var result = await handler.Handle(new GetInventoryValuationReportQuery(IncludeInactive: true), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task InventoryValuation_FiltersToSpecificWarehouse()
    {
        // Arrange
        using var ctx = TestDbContextFactory.Create();

        var cat = new Category("Office");
        ctx.Categories.Add(cat);

        var wh1 = new Warehouse("WH-1", "Cairo");
        var wh2 = new Warehouse("WH-2", "Giza");
        ctx.Warehouses.AddRange(wh1, wh2);
        await ctx.SaveChangesAsync();

        var p = new Product("Chair", "CHR-001", 200m, 2, cat.Id);
        ctx.Products.Add(p);
        await ctx.SaveChangesAsync();

        ctx.InventoryItems.AddRange(
            new InventoryItem(wh1.Id, p.Id, 7),
            new InventoryItem(wh2.Id, p.Id, 3)
        );
        await ctx.SaveChangesAsync();

        var handler = new GetInventoryValuationReportQueryHandler(ctx);

        // Act — filter to wh1 only
        var result = await handler.Handle(new GetInventoryValuationReportQuery(WarehouseId: wh1.Id), CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(wh1.Id, result[0].WarehouseId);
        Assert.Equal(7, result[0].TotalUnitsOnHand);
    }

    [Fact]
    public async Task InventoryValuation_ReturnsEmpty_WhenNoInventoryItems()
    {
        // Arrange
        using var ctx = TestDbContextFactory.Create();
        var handler = new GetInventoryValuationReportQueryHandler(ctx);

        // Act
        var result = await handler.Handle(new GetInventoryValuationReportQuery(), CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetSalesOrderSummaryReportQueryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SalesOrderSummary_ReturnsCorrectAggregateCounts()
    {
        // Arrange
        using var ctx = TestDbContextFactory.Create();

        var cat = new Category("Misc");
        ctx.Categories.Add(cat);
        var customer = new Customer("Acme Corp", "acme@example.com", "01001234567");
        ctx.Customers.Add(customer);
        var wh = new Warehouse("HQ", "Cairo");
        ctx.Warehouses.Add(wh);
        await ctx.SaveChangesAsync();

        var prod = new Product("Widget", "WGT-RPT", 100m, 0, cat.Id);
        ctx.Products.Add(prod);
        await ctx.SaveChangesAsync();

        // InventoryItem needed so AddItem doesn't fail stock checks in handler
        // (AddItem is a domain method that does NOT check stock — only ReserveStock does)
        ctx.InventoryItems.Add(new InventoryItem(wh.Id, prod.Id, 500));
        await ctx.SaveChangesAsync();

        // Create orders — Confirm() requires at least one item
        var orders = new[]
        {
            new SalesOrder($"SO-{Guid.NewGuid():N}", customer.Id, wh.Id),
            new SalesOrder($"SO-{Guid.NewGuid():N}", customer.Id, wh.Id),
            new SalesOrder($"SO-{Guid.NewGuid():N}", customer.Id, wh.Id),
            new SalesOrder($"SO-{Guid.NewGuid():N}", customer.Id, wh.Id),
            new SalesOrder($"SO-{Guid.NewGuid():N}", customer.Id, wh.Id),
        };
        // Add a line item to all orders that will be confirmed/completed
        foreach (var o in orders.Take(4))
            o.AddItem(prod.Id, 1, 100m);

        // orders[0] stays Pending
        orders[1].Confirm("manager");
        orders[2].Confirm("manager"); orders[2].Complete("manager");
        orders[3].Confirm("manager"); orders[3].Complete("manager");
        orders[4].Cancel(); // no items needed for Cancel

        ctx.SalesOrders.AddRange(orders);
        await ctx.SaveChangesAsync();

        // Patch total amounts via EF Entry
        foreach (var (so, amount) in new[] { (orders[0], 500m), (orders[1], 300m),
                                              (orders[2], 800m), (orders[3], 1200m), (orders[4], 100m) })
        {
            ctx.Entry(so).Property("TotalAmount").CurrentValue = amount;
        }
        await ctx.SaveChangesAsync();

        var handler = new GetSalesOrderSummaryReportQueryHandler(ctx);

        // Act
        var result = await handler.Handle(new GetSalesOrderSummaryReportQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(5,    result.TotalOrders);
        Assert.Equal(1,    result.PendingOrders);
        Assert.Equal(1,    result.ConfirmedOrders);
        Assert.Equal(2,    result.CompletedOrders);
        Assert.Equal(1,    result.CancelledOrders);
        Assert.Equal(2900m, result.TotalRevenue);      // 500+300+800+1200+100
        Assert.Equal(2000m, result.CompletedRevenue);  // 800+1200
        Assert.Equal(580m,  result.AverageOrderValue); // 2900/5
    }

    [Fact]
    public async Task SalesOrderSummary_ReturnsZeroTotals_WhenNoOrdersInRange()
    {
        // Arrange
        using var ctx = TestDbContextFactory.Create();
        var handler = new GetSalesOrderSummaryReportQueryHandler(ctx);

        var from = DateTimeOffset.UtcNow.AddDays(-10);
        var to   = DateTimeOffset.UtcNow;

        // Act — empty DB
        var result = await handler.Handle(new GetSalesOrderSummaryReportQuery(From: from, To: to), CancellationToken.None);

        // Assert
        Assert.Equal(0,  result.TotalOrders);
        Assert.Equal(0m, result.TotalRevenue);
        Assert.Equal(0m, result.AverageOrderValue);
        Assert.Empty(result.TopCustomers);
    }

    [Fact]
    public async Task SalesOrderSummary_TopCustomersExcludesCancelledOrders()
    {
        // Arrange
        using var ctx = TestDbContextFactory.Create();

        var cat = new Category("Misc");
        ctx.Categories.Add(cat);
        var customer = new Customer("Spender Ltd", "spend@example.com", "01009999999");
        ctx.Customers.Add(customer);
        var wh = new Warehouse("WH", "Cairo");
        ctx.Warehouses.Add(wh);
        await ctx.SaveChangesAsync();

        var prod = new Product("Gadget", "GDG-RPT", 50m, 0, cat.Id);
        ctx.Products.Add(prod);
        await ctx.SaveChangesAsync();

        // Completed order — needs an item before Confirm()
        var soCompleted = new SalesOrder($"SO-{Guid.NewGuid():N}", customer.Id, wh.Id);
        soCompleted.AddItem(prod.Id, 1, 50m);
        soCompleted.Confirm("mgr"); soCompleted.Complete("mgr");

        // Cancelled order — no items needed
        var soCancelled = new SalesOrder($"SO-{Guid.NewGuid():N}", customer.Id, wh.Id);
        soCancelled.Cancel();

        ctx.SalesOrders.AddRange(soCompleted, soCancelled);
        await ctx.SaveChangesAsync();

        ctx.Entry(soCompleted).Property("TotalAmount").CurrentValue = 1000m;
        ctx.Entry(soCancelled).Property("TotalAmount").CurrentValue = 5000m;
        await ctx.SaveChangesAsync();

        var handler = new GetSalesOrderSummaryReportQueryHandler(ctx);

        // Act
        var result = await handler.Handle(new GetSalesOrderSummaryReportQuery(), CancellationToken.None);

        // Assert — Cancelled order excluded from top-customer calculation
        Assert.Single(result.TopCustomers);
        Assert.Equal(1000m, result.TopCustomers[0].TotalSpend); // only completed counted
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetPurchaseOrderSummaryReportQueryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PurchaseOrderSummary_ReturnsCorrectStatusCounts()
    {
        // Arrange
        using var ctx = TestDbContextFactory.Create();

        var supplier = new Supplier("TechParts Co", "s@s.com", "01234567890");
        ctx.Suppliers.Add(supplier);
        var wh = new Warehouse("HQ", "Cairo");
        ctx.Warehouses.Add(wh);
        var cat = new Category("Parts");
        ctx.Categories.Add(cat);
        await ctx.SaveChangesAsync();

        var prod = new Product("Bolt", "BLT-001", 5m, 10, cat.Id);
        ctx.Products.Add(prod);
        await ctx.SaveChangesAsync();

        // po1 stays in Draft (no items needed for Draft)
        var po1 = new PurchaseOrder($"PO-{Guid.NewGuid():N}", supplier.Id, wh.Id);

        // po2 → PendingApproval: needs item before SubmitForApproval
        var po2 = new PurchaseOrder($"PO-{Guid.NewGuid():N}", supplier.Id, wh.Id);
        po2.AddItem(prod.Id, 50, 5m);
        po2.SubmitForApproval();

        // po3 → Approved: needs item before Approve
        var po3 = new PurchaseOrder($"PO-{Guid.NewGuid():N}", supplier.Id, wh.Id);
        po3.AddItem(prod.Id, 10, 5m);
        po3.Approve("admin");

        // po4 → Received: needs item, Approve, then MarkAsReceived
        var po4 = new PurchaseOrder($"PO-{Guid.NewGuid():N}", supplier.Id, wh.Id);
        po4.AddItem(prod.Id, 20, 5m);
        po4.Approve("admin");
        po4.MarkAsReceived("staff");

        // po5 → Cancelled: Cancel from Draft (no items needed)
        var po5 = new PurchaseOrder($"PO-{Guid.NewGuid():N}", supplier.Id, wh.Id);
        po5.Cancel();

        ctx.PurchaseOrders.AddRange(po1, po2, po3, po4, po5);
        await ctx.SaveChangesAsync();

        // Patch total amounts via EF Entry
        foreach (var (po, amount) in new[] { (po1, 200m), (po2, 300m), (po3, 500m), (po4, 800m), (po5, 100m) })
        {
            ctx.Entry(po).Property("TotalAmount").CurrentValue = amount;
        }
        await ctx.SaveChangesAsync();

        var handler = new GetPurchaseOrderSummaryReportQueryHandler(ctx);

        // Act
        var result = await handler.Handle(new GetPurchaseOrderSummaryReportQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(5,     result.TotalOrders);
        Assert.Equal(1,     result.DraftOrders);
        Assert.Equal(1,     result.PendingApprovalOrders);
        Assert.Equal(1,     result.ApprovedOrders);
        Assert.Equal(1,     result.ReceivedOrders);
        Assert.Equal(1,     result.CancelledOrders);
        Assert.Equal(1900m, result.TotalSpend);
        Assert.Equal(800m,  result.ReceivedSpend);
        Assert.Equal(380m,  result.AverageOrderValue); // 1900/5
    }

    [Fact]
    public async Task PurchaseOrderSummary_TopSuppliers_RankedBySpend()
    {
        // Arrange
        using var ctx = TestDbContextFactory.Create();

        var s1 = new Supplier("Big Supplier",   "big@s.com",   "01001111111");
        var s2 = new Supplier("Small Supplier", "small@s.com", "01002222222");
        ctx.Suppliers.AddRange(s1, s2);
        var wh = new Warehouse("HQ", "Cairo");
        ctx.Warehouses.Add(wh);
        var cat = new Category("Hardware");
        ctx.Categories.Add(cat);
        await ctx.SaveChangesAsync();

        var prod = new Product("Nut", "NUT-001", 2m, 5, cat.Id);
        ctx.Products.Add(prod);
        await ctx.SaveChangesAsync();

        // Approve and Receive require items
        var po1 = new PurchaseOrder($"PO-{Guid.NewGuid():N}", s1.Id, wh.Id);
        po1.AddItem(prod.Id, 100, 2m);
        po1.Approve("admin");
        po1.MarkAsReceived("staff");

        var po2 = new PurchaseOrder($"PO-{Guid.NewGuid():N}", s1.Id, wh.Id);
        po2.AddItem(prod.Id, 50, 2m);
        po2.Approve("admin");
        po2.MarkAsReceived("staff");

        var po3 = new PurchaseOrder($"PO-{Guid.NewGuid():N}", s2.Id, wh.Id);
        po3.AddItem(prod.Id, 20, 2m);
        po3.Approve("admin");

        ctx.PurchaseOrders.AddRange(po1, po2, po3);
        await ctx.SaveChangesAsync();

        ctx.Entry(po1).Property("TotalAmount").CurrentValue = 5000m;
        ctx.Entry(po2).Property("TotalAmount").CurrentValue = 3000m;
        ctx.Entry(po3).Property("TotalAmount").CurrentValue = 1000m;
        await ctx.SaveChangesAsync();

        var handler = new GetPurchaseOrderSummaryReportQueryHandler(ctx);

        // Act
        var result = await handler.Handle(new GetPurchaseOrderSummaryReportQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TopSuppliers.Count);
        Assert.Equal(s1.Id, result.TopSuppliers[0].SupplierId);
        Assert.Equal(8000m, result.TopSuppliers[0].TotalSpend); // 5000+3000
        Assert.Equal(s2.Id, result.TopSuppliers[1].SupplierId);
        Assert.Equal(1000m, result.TopSuppliers[1].TotalSpend);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetTopSellingProductsReportQueryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TopSellingProducts_RanksProductsByQuantitySold()
    {
        // Arrange
        using var ctx = TestDbContextFactory.Create();

        var cat      = new Category("Electronics");
        ctx.Categories.Add(cat);
        var customer = new Customer("RetailMart", "r@m.com", "01003333333");
        ctx.Customers.Add(customer);
        var wh = new Warehouse("HQ", "Cairo");
        ctx.Warehouses.Add(wh);
        await ctx.SaveChangesAsync();

        var p1 = new Product("Laptop", "LAP-001", 1000m, 5, cat.Id);
        var p2 = new Product("Tablet", "TAB-001", 600m,  3, cat.Id);
        ctx.Products.AddRange(p1, p2);
        await ctx.SaveChangesAsync();

        // Create inventory items so AddItem domain invariant doesn't fail (no stock check needed for SO in tests)
        ctx.InventoryItems.AddRange(
            new InventoryItem(wh.Id, p1.Id, 100),
            new InventoryItem(wh.Id, p2.Id, 100)
        );
        await ctx.SaveChangesAsync();

        // Use SalesOrder domain constructor; items via AddItem
        var so = new SalesOrder($"SO-{Guid.NewGuid():N}", customer.Id, wh.Id);
        so.AddItem(p1.Id, 10, 1000m);
        so.AddItem(p2.Id, 4,  600m);
        so.Confirm("mgr");
        so.Complete("mgr");
        ctx.SalesOrders.Add(so);
        await ctx.SaveChangesAsync();

        var handler = new GetTopSellingProductsReportQueryHandler(ctx);

        // Act
        var result = await handler.Handle(new GetTopSellingProductsReportQuery(TopN: 10), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Products.Count);
        Assert.Equal(1,        result.Products[0].Rank);
        Assert.Equal("Laptop", result.Products[0].ProductName);
        Assert.Equal(10,       result.Products[0].TotalQuantitySold);
        Assert.Equal(10000m,   result.Products[0].TotalRevenue);
        Assert.Equal(2,        result.Products[1].Rank);
        Assert.Equal("Tablet", result.Products[1].ProductName);
        Assert.Equal(4,        result.Products[1].TotalQuantitySold);
    }

    [Fact]
    public async Task TopSellingProducts_ExcludesPendingAndCancelledOrders()
    {
        // Arrange
        using var ctx = TestDbContextFactory.Create();

        var cat      = new Category("Misc");
        ctx.Categories.Add(cat);
        var customer = new Customer("TestCo", "t@c.com", "01004444444");
        ctx.Customers.Add(customer);
        var wh = new Warehouse("HQ", "Cairo");
        ctx.Warehouses.Add(wh);
        await ctx.SaveChangesAsync();

        var product = new Product("Widget", "WGT-001", 50m, 1, cat.Id);
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();

        ctx.InventoryItems.Add(new InventoryItem(wh.Id, product.Id, 100));
        await ctx.SaveChangesAsync();

        // Completed order → counted
        var soCompleted = new SalesOrder($"SO-{Guid.NewGuid():N}", customer.Id, wh.Id);
        soCompleted.AddItem(product.Id, 10, 50m);
        soCompleted.Confirm("mgr"); soCompleted.Complete("mgr");

        // Pending order → NOT counted
        var soPending = new SalesOrder($"SO-{Guid.NewGuid():N}", customer.Id, wh.Id);
        soPending.AddItem(product.Id, 5, 50m);

        ctx.SalesOrders.AddRange(soCompleted, soPending);
        await ctx.SaveChangesAsync();

        var handler = new GetTopSellingProductsReportQueryHandler(ctx);

        // Act
        var result = await handler.Handle(new GetTopSellingProductsReportQuery(), CancellationToken.None);

        // Assert — only completed qty counted (10, not 15)
        Assert.Single(result.Products);
        Assert.Equal(10, result.Products[0].TotalQuantitySold);
    }

    [Fact]
    public async Task TopSellingProducts_ReturnsEmpty_WhenNoCompletedOrders()
    {
        // Arrange
        using var ctx = TestDbContextFactory.Create();
        var handler = new GetTopSellingProductsReportQueryHandler(ctx);

        // Act
        var result = await handler.Handle(new GetTopSellingProductsReportQuery(), CancellationToken.None);

        // Assert
        Assert.Empty(result.Products);
    }

    [Fact]
    public async Task TopSellingProducts_RespectsTopNLimit()
    {
        // Arrange
        using var ctx = TestDbContextFactory.Create();

        var cat      = new Category("Furniture");
        ctx.Categories.Add(cat);
        var customer = new Customer("BulkBuy", "b@buy.com", "01005555555");
        ctx.Customers.Add(customer);
        var wh = new Warehouse("HQ", "Cairo");
        ctx.Warehouses.Add(wh);
        await ctx.SaveChangesAsync();

        // 5 different products
        var products = new List<Product>();
        for (int i = 1; i <= 5; i++)
            products.Add(new Product($"Item-{i}", $"ITM-00{i}", 100m, 1, cat.Id));
        ctx.Products.AddRange(products);
        await ctx.SaveChangesAsync();

        // Add inventory for each
        ctx.InventoryItems.AddRange(products.Select(p => new InventoryItem(wh.Id, p.Id, 100)));
        await ctx.SaveChangesAsync();

        // One completed order with all products
        var so = new SalesOrder($"SO-{Guid.NewGuid():N}", customer.Id, wh.Id);
        for (int idx = 0; idx < products.Count; idx++)
            so.AddItem(products[idx].Id, idx + 1, 100m); // Different quantities so ranking is deterministic
        so.Confirm("mgr"); so.Complete("mgr");
        ctx.SalesOrders.Add(so);
        await ctx.SaveChangesAsync();

        var handler = new GetTopSellingProductsReportQueryHandler(ctx);

        // Act — request TopN = 3
        var result = await handler.Handle(new GetTopSellingProductsReportQuery(TopN: 3), CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Products.Count);
        Assert.Equal(3, result.TopN);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetWarehouseUtilizationReportQueryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task WarehouseUtilization_FlagsLowStockAndOutOfStock_Correctly()
    {
        // Arrange
        using var ctx = TestDbContextFactory.Create();

        var cat = new Category("Parts");
        ctx.Categories.Add(cat);
        var wh = new Warehouse("Test-WH", "Cairo");
        ctx.Warehouses.Add(wh);
        await ctx.SaveChangesAsync();

        // MinimumStockLevel=10; Quantity=5 → LowStock, not OutOfStock
        var pLow = new Product("LowItem",  "LOW-001", 100m, 10, cat.Id);
        // MinimumStockLevel=3;  Quantity=0 → OutOfStock (and also LowStock)
        var pOut = new Product("OutItem",  "OUT-001", 200m, 3,  cat.Id);
        // MinimumStockLevel=5;  Quantity=20 → Normal
        var pOk  = new Product("OkayItem", "OKY-001", 50m,  5,  cat.Id);
        ctx.Products.AddRange(pLow, pOut, pOk);
        await ctx.SaveChangesAsync();

        var iLow = new InventoryItem(wh.Id, pLow.Id, 5);
        var iOut = new InventoryItem(wh.Id, pOut.Id, 0);    // 0-qty constructor not valid; use 1 then remove
        var iOk  = new InventoryItem(wh.Id, pOk.Id,  20);

        // InventoryItem(warehouseId, productId, initialQuantity=0) is valid with 0
        ctx.InventoryItems.AddRange(iLow, iOut, iOk);
        await ctx.SaveChangesAsync();

        // Reserve 2 units on iOk to test available qty
        iOk.ReserveStock(2);
        await ctx.SaveChangesAsync();

        var handler = new GetWarehouseUtilizationReportQueryHandler(ctx);

        // Act
        var result = await handler.Handle(new GetWarehouseUtilizationReportQuery(IncludeProductDetails: true), CancellationToken.None);

        // Assert
        Assert.Single(result.Warehouses);
        var entry = result.Warehouses[0];

        Assert.Equal(3,      entry.TotalDistinctProducts);
        Assert.Equal(25,     entry.TotalUnitsOnHand);         // 5+0+20
        Assert.Equal(2,      entry.TotalUnitsReserved);
        Assert.Equal(23,     entry.TotalUnitsAvailable);      // 25-2
        Assert.Equal(2,      entry.LowStockProductCount);     // pLow + pOut
        Assert.Equal(1,      entry.OutOfStockProductCount);   // pOut only (qty == 0)
        Assert.Equal(1500m,  entry.TotalStockValue);          // 5*100 + 0*200 + 20*50

        Assert.Equal(3, entry.Products.Count);

        var lowItem = entry.Products.First(p => p.SKU == "LOW-001");
        Assert.True(lowItem.IsLowStock);
        Assert.False(lowItem.IsOutOfStock);

        var outItem = entry.Products.First(p => p.SKU == "OUT-001");
        Assert.True(outItem.IsLowStock);
        Assert.True(outItem.IsOutOfStock);

        var okItem = entry.Products.First(p => p.SKU == "OKY-001");
        Assert.False(okItem.IsLowStock);
        Assert.False(okItem.IsOutOfStock);
    }

    [Fact]
    public async Task WarehouseUtilization_ReturnsEmptyProductList_WhenIncludeProductDetailsFalse()
    {
        // Arrange
        using var ctx = TestDbContextFactory.Create();

        var cat = new Category("Misc");
        ctx.Categories.Add(cat);
        var wh = new Warehouse("Slim-WH", "Giza");
        ctx.Warehouses.Add(wh);
        await ctx.SaveChangesAsync();

        var p = new Product("Bolt", "BLT-001", 5m, 10, cat.Id);
        ctx.Products.Add(p);
        await ctx.SaveChangesAsync();

        ctx.InventoryItems.Add(new InventoryItem(wh.Id, p.Id, 100));
        await ctx.SaveChangesAsync();

        var handler = new GetWarehouseUtilizationReportQueryHandler(ctx);

        // Act
        var result = await handler.Handle(new GetWarehouseUtilizationReportQuery(IncludeProductDetails: false), CancellationToken.None);

        // Assert — totals present; Products list is empty
        Assert.Single(result.Warehouses);
        Assert.Equal(1,   result.Warehouses[0].TotalDistinctProducts);
        Assert.Equal(100, result.Warehouses[0].TotalUnitsOnHand);
        Assert.Empty(result.Warehouses[0].Products);
    }

    [Fact]
    public async Task WarehouseUtilization_ExcludesInactiveWarehouses()
    {
        // Arrange
        using var ctx = TestDbContextFactory.Create();

        var cat = new Category("Hardware");
        ctx.Categories.Add(cat);

        var activeWh   = new Warehouse("Active-WH",   "Cairo");
        var inactiveWh = new Warehouse("Inactive-WH", "Alex");
        ctx.Warehouses.AddRange(activeWh, inactiveWh);
        await ctx.SaveChangesAsync();

        inactiveWh.Deactivate();

        var p = new Product("Screw", "SCR-001", 2m, 50, cat.Id);
        ctx.Products.Add(p);
        await ctx.SaveChangesAsync();

        ctx.InventoryItems.AddRange(
            new InventoryItem(activeWh.Id,   p.Id, 200),
            new InventoryItem(inactiveWh.Id, p.Id, 999)
        );
        await ctx.SaveChangesAsync();

        var handler = new GetWarehouseUtilizationReportQueryHandler(ctx);

        // Act
        var result = await handler.Handle(new GetWarehouseUtilizationReportQuery(), CancellationToken.None);

        // Assert — only active warehouse
        Assert.Single(result.Warehouses);
        Assert.Equal(activeWh.Id, result.Warehouses[0].WarehouseId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Validator tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SalesOrderSummaryValidator_RejectsFromAfterTo()
    {
        var validator = new GetSalesOrderSummaryReportQueryValidator();
        var query     = new GetSalesOrderSummaryReportQuery(
            From: DateTimeOffset.UtcNow,
            To:   DateTimeOffset.UtcNow.AddDays(-1)
        );
        var result = validator.Validate(query);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "From");
    }

    [Fact]
    public void SalesOrderSummaryValidator_RejectsTopCustomersCountOutOfRange()
    {
        var validator = new GetSalesOrderSummaryReportQueryValidator();
        var query     = new GetSalesOrderSummaryReportQuery(TopCustomersCount: 0);
        var result    = validator.Validate(query);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TopCustomersCount");
    }

    [Fact]
    public void PurchaseOrderSummaryValidator_RejectsTopSuppliersCountAboveMax()
    {
        var validator = new GetPurchaseOrderSummaryReportQueryValidator();
        var query     = new GetPurchaseOrderSummaryReportQuery(TopSuppliersCount: 51);
        var result    = validator.Validate(query);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TopSuppliersCount");
    }

    [Fact]
    public void TopSellingProductsValidator_RejectsTopNAboveMax()
    {
        var validator = new GetTopSellingProductsReportQueryValidator();
        var query     = new GetTopSellingProductsReportQuery(TopN: 101);
        var result    = validator.Validate(query);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TopN");
    }

    [Fact]
    public void TopSellingProductsValidator_AcceptsValidQuery()
    {
        var validator = new GetTopSellingProductsReportQueryValidator();
        var query     = new GetTopSellingProductsReportQuery(
            From: DateTimeOffset.UtcNow.AddDays(-30),
            To:   DateTimeOffset.UtcNow.AddMinutes(-1),
            TopN: 25
        );
        var result = validator.Validate(query);
        Assert.True(result.IsValid);
    }
}
