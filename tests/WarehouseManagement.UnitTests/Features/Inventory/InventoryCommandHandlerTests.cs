using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Features.Inventory.Commands.AddStock;
using WarehouseManagement.Application.Features.Inventory.Commands.AdjustStock;
using WarehouseManagement.Application.Features.Inventory.Commands.ReleaseStock;
using WarehouseManagement.Application.Features.Inventory.Commands.RemoveStock;
using WarehouseManagement.Application.Features.Inventory.Commands.ReserveStock;
using WarehouseManagement.Application.Features.Inventory.Commands.TransferStock;
using WarehouseManagement.Application.Features.Inventory.Queries.GetLowStockProducts;
using WarehouseManagement.Application.Features.Inventory.Queries.GetProductStock;
using WarehouseManagement.Application.Features.Inventory.Queries.GetStockTransactions;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Domain.Enums;
using WarehouseManagement.Domain.Exceptions;
using WarehouseManagement.UnitTests.Common;

namespace WarehouseManagement.UnitTests.Features.Inventory;

public class InventoryCommandHandlerTests
{
    private readonly TestCurrentUserService _currentUserService = new();

    [Fact]
    public async Task AddStock_ToNewProductInWarehouse_ShouldCreateInventoryItemAndStockInTransaction()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Lighting");
        context.Categories.Add(category);

        var product = new Product("LED Flood Light 100W", "LGT-FLD-100", 45.00m, 5, category.Id);
        context.Products.Add(product);

        var warehouse = new Warehouse("Alexandria Port Facility", "Gate 5, Port of Alexandria");
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var handler = new AddStockCommandHandler(context, _currentUserService);
        var command = new AddStockCommand(warehouse.Id, product.Id, Quantity: 80, ReferenceId: "INITIAL-PO-01", Notes: "Opening stock");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(80, result.Quantity);
        Assert.Equal(0, result.ReservedQuantity);
        Assert.Equal(80, result.AvailableQuantity);

        var tx = await context.StockTransactions.FirstOrDefaultAsync();
        Assert.NotNull(tx);
        Assert.Equal(StockTransactionType.StockIn, tx.Type);
        Assert.Equal(80, tx.Quantity);
        Assert.Equal("INITIAL-PO-01", tx.ReferenceId);
        Assert.Equal("test_manager", tx.CreatedBy);
    }

    [Fact]
    public async Task AddStock_ToExistingProduct_ShouldIncrementQuantityAndRecordTransaction()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Cables");
        context.Categories.Add(category);

        var product = new Product("Cat6 Ethernet Reel 305m", "CBL-CAT6-305", 95.00m, 10, category.Id);
        context.Products.Add(product);

        var warehouse = new Warehouse("Cairo Logistics Hub", "Nasr City");
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var existingItem = new InventoryItem(warehouse.Id, product.Id, initialQuantity: 20);
        context.InventoryItems.Add(existingItem);
        await context.SaveChangesAsync();

        var handler = new AddStockCommandHandler(context, _currentUserService);
        var command = new AddStockCommand(warehouse.Id, product.Id, Quantity: 30, ReferenceId: "PO-2026-004");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(50, result.Quantity);
        Assert.Equal(50, result.AvailableQuantity);

        var tx = await context.StockTransactions.FirstOrDefaultAsync();
        Assert.NotNull(tx);
        Assert.Equal(StockTransactionType.StockIn, tx.Type);
        Assert.Equal(30, tx.Quantity);
    }

    [Fact]
    public async Task AddStock_ToDeactivatedWarehouse_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Hardware");
        context.Categories.Add(category);

        var product = new Product("Nail Gun", "TOOL-NG-01", 120m, 5, category.Id);
        context.Products.Add(product);

        var warehouse = new Warehouse("Closed Depot", "Desert Road");
        warehouse.Deactivate();
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var handler = new AddStockCommandHandler(context, _currentUserService);
        var command = new AddStockCommand(warehouse.Id, product.Id, Quantity: 10);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("deactivated warehouse", ex.Message);
    }

    [Fact]
    public async Task RemoveStock_WithSufficientQuantity_ShouldDecrementQuantityAndRecordStockOut()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Safety Gear");
        context.Categories.Add(category);

        var product = new Product("Safety Helmet Yellow", "HLM-YL-01", 15m, 20, category.Id);
        context.Products.Add(product);

        var warehouse = new Warehouse("Industrial Zone Depot", "Sadat City");
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var item = new InventoryItem(warehouse.Id, product.Id, initialQuantity: 100);
        context.InventoryItems.Add(item);
        await context.SaveChangesAsync();

        var handler = new RemoveStockCommandHandler(context, _currentUserService);
        var command = new RemoveStockCommand(warehouse.Id, product.Id, Quantity: 25, ReferenceId: "DAMAGED-WRITEOFF-01", Notes: "Water damage writeoff");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(75, result.Quantity);
        Assert.Equal(75, result.AvailableQuantity);

        var tx = await context.StockTransactions.FirstOrDefaultAsync();
        Assert.NotNull(tx);
        Assert.Equal(StockTransactionType.StockOut, tx.Type);
        Assert.Equal(25, tx.Quantity);
        Assert.Equal("DAMAGED-WRITEOFF-01", tx.ReferenceId);
    }

    [Fact]
    public async Task RemoveStock_WithInsufficientQuantity_ShouldThrowInsufficientStockException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Tools");
        context.Categories.Add(category);

        var product = new Product("Angle Grinder", "TOOL-AG-01", 80m, 5, category.Id);
        context.Products.Add(product);

        var warehouse = new Warehouse("Suez Facility", "Port Tawfik");
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var item = new InventoryItem(warehouse.Id, product.Id, initialQuantity: 10);
        context.InventoryItems.Add(item);
        await context.SaveChangesAsync();

        var handler = new RemoveStockCommandHandler(context, _currentUserService);
        var command = new RemoveStockCommand(warehouse.Id, product.Id, Quantity: 20); // More than available

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task TransferStock_BetweenWarehouses_ShouldExecuteAtomicallyAndRecordTransferTransaction()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Industrial Packaging");
        context.Categories.Add(category);

        var product = new Product("Stretch Film Roll 500mm", "PKG-FLM-500", 18.00m, 50, category.Id);
        context.Products.Add(product);

        var sourceWarehouse = new Warehouse("Central Distribution Cairo", "Abu Rawash");
        var destWarehouse = new Warehouse("Regional Hub Mansoura", "Mansoura Ring Road");
        context.Warehouses.AddRange(sourceWarehouse, destWarehouse);
        await context.SaveChangesAsync();

        var sourceItem = new InventoryItem(sourceWarehouse.Id, product.Id, initialQuantity: 150);
        context.InventoryItems.Add(sourceItem);
        await context.SaveChangesAsync();

        var handler = new TransferStockCommandHandler(context, _currentUserService);
        var command = new TransferStockCommand(
            FromWarehouseId: sourceWarehouse.Id,
            ToWarehouseId: destWarehouse.Id,
            ProductId: product.Id,
            Quantity: 50,
            ReferenceId: "TRF-2026-001",
            Notes: "Replenishment transfer");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(50, result.Quantity);
        Assert.Equal(sourceWarehouse.Name, result.FromWarehouseName);
        Assert.Equal(destWarehouse.Name, result.ToWarehouseName);

        // Verify source stock decremented
        var updatedSource = await context.InventoryItems.FirstAsync(i => i.WarehouseId == sourceWarehouse.Id && i.ProductId == product.Id);
        Assert.Equal(100, updatedSource.Quantity);

        // Verify destination stock incremented
        var updatedDest = await context.InventoryItems.FirstAsync(i => i.WarehouseId == destWarehouse.Id && i.ProductId == product.Id);
        Assert.Equal(50, updatedDest.Quantity);

        // Verify transfer transaction
        var tx = await context.StockTransactions.FirstOrDefaultAsync();
        Assert.NotNull(tx);
        Assert.Equal(StockTransactionType.Transfer, tx.Type);
        Assert.Equal(sourceWarehouse.Id, tx.WarehouseId);
        Assert.Equal(destWarehouse.Id, tx.ToWarehouseId);
        Assert.Equal(50, tx.Quantity);
    }

    [Fact]
    public async Task TransferStock_ToSameWarehouse_ShouldThrowSameWarehouseTransferException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var handler = new TransferStockCommandHandler(context, _currentUserService);
        var command = new TransferStockCommand(FromWarehouseId: 1, ToWarehouseId: 1, ProductId: 10, Quantity: 5);

        // Act & Assert
        await Assert.ThrowsAsync<SameWarehouseTransferException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task TransferStock_WithInsufficientQuantity_ShouldThrowInsufficientStockException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Lubricants");
        context.Categories.Add(category);

        var product = new Product("Hydraulic Oil 20L", "OIL-HYD-20L", 65m, 10, category.Id);
        context.Products.Add(product);

        var wh1 = new Warehouse("Warehouse Alpha", "Area 1");
        var wh2 = new Warehouse("Warehouse Beta", "Area 2");
        context.Warehouses.AddRange(wh1, wh2);
        await context.SaveChangesAsync();

        var sourceItem = new InventoryItem(wh1.Id, product.Id, initialQuantity: 15);
        context.InventoryItems.Add(sourceItem);
        await context.SaveChangesAsync();

        var handler = new TransferStockCommandHandler(context, _currentUserService);
        var command = new TransferStockCommand(wh1.Id, wh2.Id, product.Id, Quantity: 30); // exceeds 15

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task GetProductStockQuery_ShouldReturnDistributionAcrossWarehouses()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Pumps");
        context.Categories.Add(category);

        var product = new Product("Submersible Water Pump", "PMP-SUB-01", 210m, 5, category.Id);
        context.Products.Add(product);

        var wh1 = new Warehouse("Cairo West", "6th of October");
        var wh2 = new Warehouse("Alexandria", "Dekheila");
        context.Warehouses.AddRange(wh1, wh2);
        await context.SaveChangesAsync();

        context.InventoryItems.AddRange(
            new InventoryItem(wh1.Id, product.Id, initialQuantity: 12),
            new InventoryItem(wh2.Id, product.Id, initialQuantity: 8)
        );
        await context.SaveChangesAsync();

        var handler = new GetProductStockQueryHandler(context);
        var query = new GetProductStockQuery(product.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(20, result.Sum(i => i.Quantity));
    }

    [Fact]
    public async Task GetStockTransactionsQuery_ShouldReturnPaginatedAuditLogs()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Chemicals");
        context.Categories.Add(category);

        var product = new Product("Degreaser Solvent 5L", "CHM-DGR-05", 35m, 20, category.Id);
        context.Products.Add(product);

        var warehouse = new Warehouse("Chemical Storage Hub", "Beni Suef");
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        context.StockTransactions.AddRange(
            StockTransaction.CreateStockIn(product.Id, warehouse.Id, 100, "PO-01", "Initial stock", "system"),
            StockTransaction.CreateStockOut(product.Id, warehouse.Id, 15, "SO-01", "Dispatch order", "staff")
        );
        await context.SaveChangesAsync();

        var handler = new GetStockTransactionsQueryHandler(context);
        var query = new GetStockTransactionsQuery(ProductId: product.Id, WarehouseId: warehouse.Id, PageNumber: 1, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task AdjustStock_PositiveDiscrepancy_ShouldIncreaseQuantityAndRecordAdjustment()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Electronics");
        context.Categories.Add(category);

        var product = new Product("Smart Sensor Node", "IOT-SEN-01", 60m, 10, category.Id);
        context.Products.Add(product);

        var warehouse = new Warehouse("Cairo Smart Village Hub", "Km 28 Cairo-Alex Desert Rd");
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var item = new InventoryItem(warehouse.Id, product.Id, initialQuantity: 40);
        context.InventoryItems.Add(item);
        await context.SaveChangesAsync();

        var handler = new AdjustStockCommandHandler(context, _currentUserService);
        // Actual physical count found 45 units (+5 discrepancy)
        var command = new AdjustStockCommand(warehouse.Id, product.Id, ActualCountedQuantity: 45, Reason: "Annual stocktake surplus");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(45, result.Quantity);
        Assert.Equal(45, result.AvailableQuantity);

        var tx = await context.StockTransactions.FirstOrDefaultAsync();
        Assert.NotNull(tx);
        Assert.Equal(StockTransactionType.Adjustment, tx.Type);
        Assert.Equal(5, tx.Quantity);
        Assert.Equal("Annual stocktake surplus", tx.Notes);
    }

    [Fact]
    public async Task AdjustStock_NegativeDiscrepancy_ShouldDecreaseQuantityAndRecordAdjustment()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Fasteners");
        context.Categories.Add(category);

        var product = new Product("Hex Bolt M10", "BLT-HEX-M10", 1.2m, 100, category.Id);
        context.Products.Add(product);

        var warehouse = new Warehouse("Helwan Logistics", "Helwan Industrial");
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var item = new InventoryItem(warehouse.Id, product.Id, initialQuantity: 200);
        context.InventoryItems.Add(item);
        await context.SaveChangesAsync();

        var handler = new AdjustStockCommandHandler(context, _currentUserService);
        // Actual physical count found 190 units (-10 discrepancy)
        var command = new AdjustStockCommand(warehouse.Id, product.Id, ActualCountedQuantity: 190, Reason: "Cycle count breakage loss");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(190, result.Quantity);
        Assert.Equal(190, result.AvailableQuantity);

        var tx = await context.StockTransactions.FirstOrDefaultAsync();
        Assert.NotNull(tx);
        Assert.Equal(StockTransactionType.Adjustment, tx.Type);
        Assert.Equal(-10, tx.Quantity);
    }

    [Fact]
    public async Task AdjustStock_NegativeDiscrepancyExceedingAvailableStock_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Appliances");
        context.Categories.Add(category);

        var product = new Product("Air Cooler 50L", "APP-AC-50L", 280m, 5, category.Id);
        context.Products.Add(product);

        var warehouse = new Warehouse("Alexandria Main Depot", "Smouha");
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        // Total 20 units, 15 are reserved for orders -> Available = 5
        var item = new InventoryItem(warehouse.Id, product.Id, initialQuantity: 20);
        item.ReserveStock(15);
        context.InventoryItems.Add(item);
        await context.SaveChangesAsync();

        var handler = new AdjustStockCommandHandler(context, _currentUserService);
        // Trying to adjust physical count to 10 (deduction of 10), but only 5 are unreserved!
        var command = new AdjustStockCommand(warehouse.Id, product.Id, ActualCountedQuantity: 10, Reason: "Audit adjustment");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("currently reserved for pending orders", ex.Message);
    }

    [Fact]
    public async Task ReserveStock_WithSufficientAvailableStock_ShouldIncreaseReservedQuantityAndDecreaseAvailable()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Packaging");
        context.Categories.Add(category);

        var product = new Product("Packing Peanuts 10kg", "PKG-PNT-10", 30m, 10, category.Id);
        context.Products.Add(product);

        var warehouse = new Warehouse("Obour City Warehouse", "Industrial Zone B");
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var item = new InventoryItem(warehouse.Id, product.Id, initialQuantity: 50);
        context.InventoryItems.Add(item);
        await context.SaveChangesAsync();

        var handler = new ReserveStockCommandHandler(context);
        var command = new ReserveStockCommand(warehouse.Id, product.Id, Quantity: 20);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(50, result.Quantity); // Total physical stock remains 50
        Assert.Equal(20, result.ReservedQuantity); // 20 units reserved
        Assert.Equal(30, result.AvailableQuantity); // Only 30 available for other orders
    }

    [Fact]
    public async Task ReserveStock_ExceedingAvailableQuantity_ShouldThrowInsufficientStockException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Tools");
        context.Categories.Add(category);

        var product = new Product("Impact Drill", "TOOL-ID-01", 110m, 5, category.Id);
        context.Products.Add(product);

        var warehouse = new Warehouse("Delta Depot", "Mahalla");
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var item = new InventoryItem(warehouse.Id, product.Id, initialQuantity: 10);
        context.InventoryItems.Add(item);
        await context.SaveChangesAsync();

        var handler = new ReserveStockCommandHandler(context);
        var command = new ReserveStockCommand(warehouse.Id, product.Id, Quantity: 15); // Exceeds available 10

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task ReleaseStock_WithSufficientReservedStock_ShouldDecreaseReservedAndIncreaseAvailable()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Office");
        context.Categories.Add(category);

        var product = new Product("A4 Paper Ream", "OFF-PPR-A4", 6.5m, 100, category.Id);
        context.Products.Add(product);

        var warehouse = new Warehouse("Cairo Center", "Downtown");
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var item = new InventoryItem(warehouse.Id, product.Id, initialQuantity: 100);
        item.ReserveStock(30);
        context.InventoryItems.Add(item);
        await context.SaveChangesAsync();

        var handler = new ReleaseStockCommandHandler(context);
        var command = new ReleaseStockCommand(warehouse.Id, product.Id, Quantity: 10);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(100, result.Quantity);
        Assert.Equal(20, result.ReservedQuantity); // Reserved dropped from 30 to 20
        Assert.Equal(80, result.AvailableQuantity); // Available rose from 70 to 80
    }

    [Fact]
    public async Task GetLowStockProductsQuery_ShouldIdentifyProductsBelowMinimumThreshold()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Industrial Components");
        context.Categories.Add(category);

        // Product 1: Min = 50, Stock = 30 -> LOW STOCK (Deficit = 20)
        var p1 = new Product("Hydraulic Valve", "HYD-VLV-01", 150m, minimumStockLevel: 50, category.Id);
        // Product 2: Min = 10, Stock = 100 -> HEALTHY STOCK
        var p2 = new Product("Rubber Gasket", "GSK-RBR-01", 2m, minimumStockLevel: 10, category.Id);
        context.Products.AddRange(p1, p2);

        var warehouse = new Warehouse("Main Logistics Base", "10th Ramadan");
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        context.InventoryItems.AddRange(
            new InventoryItem(warehouse.Id, p1.Id, initialQuantity: 30),
            new InventoryItem(warehouse.Id, p2.Id, initialQuantity: 100)
        );
        await context.SaveChangesAsync();

        var handler = new GetLowStockProductsQueryHandler(context);
        var query = new GetLowStockProductsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var lowStockItem = result.First();
        Assert.Equal(p1.Id, lowStockItem.ProductId);
        Assert.Equal("Hydraulic Valve", lowStockItem.ProductName);
        Assert.Equal(50, lowStockItem.MinimumStockLevel);
        Assert.Equal(30, lowStockItem.TotalAvailableStock);
        Assert.Equal(20, lowStockItem.DeficitQuantity);
    }
}
