using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Features.PurchaseOrders.Commands.ApprovePurchaseOrder;
using WarehouseManagement.Application.Features.PurchaseOrders.Commands.CancelPurchaseOrder;
using WarehouseManagement.Application.Features.PurchaseOrders.Commands.CreatePurchaseOrder;
using WarehouseManagement.Application.Features.PurchaseOrders.Commands.ReceivePurchaseOrder;
using WarehouseManagement.Application.Features.PurchaseOrders.Commands.SubmitPurchaseOrder;
using WarehouseManagement.Application.Features.PurchaseOrders.Queries.GetPurchaseOrderById;
using WarehouseManagement.Application.Features.PurchaseOrders.Queries.GetPurchaseOrders;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Domain.Enums;
using WarehouseManagement.Domain.Exceptions;
using WarehouseManagement.UnitTests.Common;

namespace WarehouseManagement.UnitTests.Features.PurchaseOrders;

public class PurchaseOrdersCommandHandlerTests
{
    private readonly TestCurrentUserService _currentUserService = new();

    private async Task<(Supplier supplier, Warehouse warehouse, Product productA, Product productB)> SeedTestDataAsync(WarehouseManagement.Infrastructure.Persistence.ApplicationDbContext context)
    {
        var category = new Category("Raw Materials");
        context.Categories.Add(category);

        var supplier = new Supplier("El-Sewedy Cables", "procurement@elsewedy.com", "+20227599700");
        context.Suppliers.Add(supplier);

        var warehouse = new Warehouse("Cairo Central Hub", "10th of Ramadan Industrial Zone");
        context.Warehouses.Add(warehouse);

        var productA = new Product("Copper Wire 2.5mm", "CBL-CU-25", 120.00m, 10, category.Id);
        var productB = new Product("Aluminum Cable 4mm", "CBL-AL-40", 85.00m, 15, category.Id);
        context.Products.AddRange(productA, productB);

        await context.SaveChangesAsync();

        return (supplier, warehouse, productA, productB);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithValidData_ShouldCreateInDraftStatusAndCalculateTotal()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (supplier, warehouse, productA, productB) = await SeedTestDataAsync(context);

        var handler = new CreatePurchaseOrderCommandHandler(context);
        var command = new CreatePurchaseOrderCommand(
            supplier.Id,
            warehouse.Id,
            new List<CreatePurchaseOrderItemInput>
            {
                new(productA.Id, Quantity: 100, UnitPrice: 110.00m),
                new(productB.Id, Quantity: 50, UnitPrice: 80.00m)
            },
            OrderNumber: "PO-SEWEDY-001"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PO-SEWEDY-001", result.OrderNumber);
        Assert.Equal(PurchaseOrderStatus.Draft.ToString(), result.Status);
        Assert.Equal(15000.00m, result.TotalAmount); // (100*110) + (50*80) = 11000 + 4000 = 15000
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(supplier.Name, result.SupplierName);
        Assert.Equal(warehouse.Name, result.WarehouseName);

        var savedPo = await context.PurchaseOrders
            .Include(po => po.Items)
            .FirstOrDefaultAsync(po => po.Id == result.Id);
        Assert.NotNull(savedPo);
        Assert.Equal(2, savedPo.Items.Count);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithDeactivatedSupplier_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (supplier, warehouse, productA, _) = await SeedTestDataAsync(context);

        supplier.Deactivate();
        await context.SaveChangesAsync();

        var handler = new CreatePurchaseOrderCommandHandler(context);
        var command = new CreatePurchaseOrderCommand(
            supplier.Id,
            warehouse.Id,
            new List<CreatePurchaseOrderItemInput>
            {
                new(productA.Id, Quantity: 10, UnitPrice: 100m)
            }
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("deactivated supplier", ex.Message);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithDeactivatedWarehouse_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (supplier, warehouse, productA, _) = await SeedTestDataAsync(context);

        warehouse.Deactivate();
        await context.SaveChangesAsync();

        var handler = new CreatePurchaseOrderCommandHandler(context);
        var command = new CreatePurchaseOrderCommand(
            supplier.Id,
            warehouse.Id,
            new List<CreatePurchaseOrderItemInput>
            {
                new(productA.Id, Quantity: 10, UnitPrice: 100m)
            }
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("deactivated destination warehouse", ex.Message);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithDuplicateProduct_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (supplier, warehouse, productA, _) = await SeedTestDataAsync(context);

        var handler = new CreatePurchaseOrderCommandHandler(context);
        var command = new CreatePurchaseOrderCommand(
            supplier.Id,
            warehouse.Id,
            new List<CreatePurchaseOrderItemInput>
            {
                new(productA.Id, Quantity: 10, UnitPrice: 100m),
                new(productA.Id, Quantity: 20, UnitPrice: 95m)
            }
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitPurchaseOrder_WhenDraft_ShouldTransitionToPendingApproval()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (supplier, warehouse, productA, _) = await SeedTestDataAsync(context);

        var po = new PurchaseOrder("PO-SUBMIT-01", supplier.Id, warehouse.Id);
        po.AddItem(productA.Id, 25, 100m);
        context.PurchaseOrders.Add(po);
        await context.SaveChangesAsync();

        var handler = new SubmitPurchaseOrderCommandHandler(context);

        // Act
        var result = await handler.Handle(new SubmitPurchaseOrderCommand(po.Id), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PurchaseOrderStatus.PendingApproval.ToString(), result.Status);

        var updatedPo = await context.PurchaseOrders.FindAsync(po.Id);
        Assert.Equal(PurchaseOrderStatus.PendingApproval, updatedPo!.Status);
    }

    [Fact]
    public async Task SubmitPurchaseOrder_WhenAlreadyPendingApproval_ShouldThrowInvalidOrderStateException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (supplier, warehouse, productA, _) = await SeedTestDataAsync(context);

        var po = new PurchaseOrder("PO-SUBMIT-02", supplier.Id, warehouse.Id);
        po.AddItem(productA.Id, 25, 100m);
        po.SubmitForApproval();
        context.PurchaseOrders.Add(po);
        await context.SaveChangesAsync();

        var handler = new SubmitPurchaseOrderCommandHandler(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOrderStateException>(() =>
            handler.Handle(new SubmitPurchaseOrderCommand(po.Id), CancellationToken.None));
    }

    [Fact]
    public async Task ApprovePurchaseOrder_WhenPendingApproval_ShouldSetApprovedStatusAndRecordApprover()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (supplier, warehouse, productA, _) = await SeedTestDataAsync(context);

        var po = new PurchaseOrder("PO-APPROVE-01", supplier.Id, warehouse.Id);
        po.AddItem(productA.Id, 40, 100m);
        po.SubmitForApproval();
        context.PurchaseOrders.Add(po);
        await context.SaveChangesAsync();

        var handler = new ApprovePurchaseOrderCommandHandler(context, _currentUserService);

        // Act
        var result = await handler.Handle(new ApprovePurchaseOrderCommand(po.Id), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PurchaseOrderStatus.Approved.ToString(), result.Status);
        Assert.NotNull(result.ApprovedAt);
        Assert.Equal("test_manager", result.ApprovedBy);

        var updatedPo = await context.PurchaseOrders.FindAsync(po.Id);
        Assert.Equal(PurchaseOrderStatus.Approved, updatedPo!.Status);
        Assert.Equal("test_manager", updatedPo.ApprovedBy);
    }

    [Fact]
    public async Task ReceivePurchaseOrder_WhenApproved_ShouldIncrementStockAndWriteAuditLedger()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (supplier, warehouse, productA, productB) = await SeedTestDataAsync(context);

        var po = new PurchaseOrder("PO-RECEIVE-01", supplier.Id, warehouse.Id);
        po.AddItem(productA.Id, 150, 110.00m);
        po.AddItem(productB.Id, 80, 80.00m);
        po.SubmitForApproval();
        po.Approve("ManagerOne");
        context.PurchaseOrders.Add(po);
        await context.SaveChangesAsync();

        var handler = new ReceivePurchaseOrderCommandHandler(context, _currentUserService);
        var command = new ReceivePurchaseOrderCommand(po.Id, Notes: "Batch received in full at bay 4");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PurchaseOrderStatus.Received.ToString(), result.Status);
        Assert.NotNull(result.ReceivedAt);
        Assert.Equal("test_manager", result.ReceivedBy);

        // 1. Verify Inventory incremented in the destination warehouse
        var itemA = await context.InventoryItems
            .FirstOrDefaultAsync(i => i.WarehouseId == warehouse.Id && i.ProductId == productA.Id);
        Assert.NotNull(itemA);
        Assert.Equal(150, itemA.Quantity);
        Assert.Equal(150, itemA.AvailableQuantity);

        var itemB = await context.InventoryItems
            .FirstOrDefaultAsync(i => i.WarehouseId == warehouse.Id && i.ProductId == productB.Id);
        Assert.NotNull(itemB);
        Assert.Equal(80, itemB.Quantity);
        Assert.Equal(80, itemB.AvailableQuantity);

        // 2. Verify immutable StockTransaction ledger entries created
        var txA = await context.StockTransactions
            .FirstOrDefaultAsync(t => t.WarehouseId == warehouse.Id && t.ProductId == productA.Id);
        Assert.NotNull(txA);
        Assert.Equal(StockTransactionType.StockIn, txA.Type);
        Assert.Equal(150, txA.Quantity);
        Assert.Equal("PO-RECEIVE-01", txA.ReferenceId);
        Assert.Equal("test_manager", txA.CreatedBy);

        var txB = await context.StockTransactions
            .FirstOrDefaultAsync(t => t.WarehouseId == warehouse.Id && t.ProductId == productB.Id);
        Assert.NotNull(txB);
        Assert.Equal(StockTransactionType.StockIn, txB.Type);
        Assert.Equal(80, txB.Quantity);
        Assert.Equal("PO-RECEIVE-01", txB.ReferenceId);

        // 3. Verify purchase order item ReceivedQuantity recorded
        var updatedPo = await context.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == po.Id);
        Assert.NotNull(updatedPo);
        Assert.All(updatedPo.Items, i => Assert.Equal(i.Quantity, i.ReceivedQuantity));
    }

    [Fact]
    public async Task ReceivePurchaseOrder_WhenStillInDraftOrPending_ShouldThrowInvalidOrderStateException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (supplier, warehouse, productA, _) = await SeedTestDataAsync(context);

        var po = new PurchaseOrder("PO-RECEIVE-FAIL", supplier.Id, warehouse.Id);
        po.AddItem(productA.Id, 10, 100m);
        context.PurchaseOrders.Add(po);
        await context.SaveChangesAsync();

        var handler = new ReceivePurchaseOrderCommandHandler(context, _currentUserService);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOrderStateException>(() =>
            handler.Handle(new ReceivePurchaseOrderCommand(po.Id), CancellationToken.None));
    }

    [Fact]
    public async Task CancelPurchaseOrder_WhenDraftOrApproved_ShouldTransitionToCancelled()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (supplier, warehouse, productA, _) = await SeedTestDataAsync(context);

        var po = new PurchaseOrder("PO-CANCEL-01", supplier.Id, warehouse.Id);
        po.AddItem(productA.Id, 30, 100m);
        context.PurchaseOrders.Add(po);
        await context.SaveChangesAsync();

        var handler = new CancelPurchaseOrderCommandHandler(context);

        // Act
        var result = await handler.Handle(new CancelPurchaseOrderCommand(po.Id), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PurchaseOrderStatus.Cancelled.ToString(), result.Status);

        var updatedPo = await context.PurchaseOrders.FindAsync(po.Id);
        Assert.Equal(PurchaseOrderStatus.Cancelled, updatedPo!.Status);
    }

    [Fact]
    public async Task CancelPurchaseOrder_WhenAlreadyReceived_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (supplier, warehouse, productA, _) = await SeedTestDataAsync(context);

        var po = new PurchaseOrder("PO-CANCEL-FAIL", supplier.Id, warehouse.Id);
        po.AddItem(productA.Id, 30, 100m);
        po.SubmitForApproval();
        po.Approve("Manager");
        po.MarkAsReceived("WarehouseStaff");
        context.PurchaseOrders.Add(po);
        await context.SaveChangesAsync();

        var handler = new CancelPurchaseOrderCommandHandler(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CancelPurchaseOrderCommand(po.Id), CancellationToken.None));

        Assert.Contains("already been received", ex.Message);
    }

    [Fact]
    public async Task GetPurchaseOrdersQuery_WithStatusFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (supplier, warehouse, productA, _) = await SeedTestDataAsync(context);

        var po1 = new PurchaseOrder("PO-FILTER-01", supplier.Id, warehouse.Id);
        po1.AddItem(productA.Id, 10, 100m);

        var po2 = new PurchaseOrder("PO-FILTER-02", supplier.Id, warehouse.Id);
        po2.AddItem(productA.Id, 20, 100m);
        po2.SubmitForApproval();

        context.PurchaseOrders.AddRange(po1, po2);
        await context.SaveChangesAsync();

        var handler = new GetPurchaseOrdersQueryHandler(context);

        // Act
        var result = await handler.Handle(new GetPurchaseOrdersQuery(Status: PurchaseOrderStatus.PendingApproval), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("PO-FILTER-02", result.Items.First().OrderNumber);
    }

    [Fact]
    public async Task GetPurchaseOrderByIdQuery_WithValidId_ShouldReturnDetails()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (supplier, warehouse, productA, _) = await SeedTestDataAsync(context);

        var po = new PurchaseOrder("PO-DETAIL-01", supplier.Id, warehouse.Id);
        po.AddItem(productA.Id, 15, 120m);
        context.PurchaseOrders.Add(po);
        await context.SaveChangesAsync();

        var handler = new GetPurchaseOrderByIdQueryHandler(context);

        // Act
        var result = await handler.Handle(new GetPurchaseOrderByIdQuery(po.Id), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PO-DETAIL-01", result.OrderNumber);
        Assert.Single(result.Items);
        Assert.Equal("Copper Wire 2.5mm", result.Items[0].ProductName);
        Assert.Equal(1800m, result.TotalAmount);
    }
}
