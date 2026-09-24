using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Features.SalesOrders.Commands.CancelSalesOrder;
using WarehouseManagement.Application.Features.SalesOrders.Commands.CompleteSalesOrder;
using WarehouseManagement.Application.Features.SalesOrders.Commands.ConfirmSalesOrder;
using WarehouseManagement.Application.Features.SalesOrders.Commands.CreateSalesOrder;
using WarehouseManagement.Application.Features.SalesOrders.Queries.GetSalesOrderById;
using WarehouseManagement.Application.Features.SalesOrders.Queries.GetSalesOrders;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Domain.Enums;
using WarehouseManagement.Domain.Exceptions;
using WarehouseManagement.UnitTests.Common;

namespace WarehouseManagement.UnitTests.Features.SalesOrders;

public class SalesOrdersCommandHandlerTests
{
    private readonly TestCurrentUserService _currentUserService = new();

    private async Task<(Customer customer, Warehouse warehouse, Product productA, Product productB)> SeedTestDataAsync(WarehouseManagement.Infrastructure.Persistence.ApplicationDbContext context)
    {
        var category = new Category("Electronics");
        context.Categories.Add(category);

        var customer = new Customer("Alexandria Electric Co", "orders@alexelectric.com", "+2034871234");
        context.Customers.Add(customer);

        var warehouse = new Warehouse("Cairo Central Hub", "10th of Ramadan Zone A");
        context.Warehouses.Add(warehouse);

        var productA = new Product("Industrial Sensor X1", "SNR-X1-001", 250.00m, 5, category.Id);
        var productB = new Product("Digital Relay Switch", "RLY-SW-002", 90.00m, 10, category.Id);
        context.Products.AddRange(productA, productB);

        await context.SaveChangesAsync();

        return (customer, warehouse, productA, productB);
    }

    [Fact]
    public async Task CreateSalesOrder_WithValidData_ShouldCreateInPendingStatusAndCalculateTotal()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (customer, warehouse, productA, productB) = await SeedTestDataAsync(context);

        var handler = new CreateSalesOrderCommandHandler(context);
        var command = new CreateSalesOrderCommand(
            customer.Id,
            warehouse.Id,
            new List<CreateSalesOrderItemInput>
            {
                new(productA.Id, Quantity: 10, UnitPrice: 240.00m),
                new(productB.Id, Quantity: 20, UnitPrice: 85.00m)
            },
            OrderNumber: "SO-ALEX-001"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SO-ALEX-001", result.OrderNumber);
        Assert.Equal(OrderStatus.Pending.ToString(), result.Status);
        Assert.Equal(4100.00m, result.TotalAmount); // (10*240) + (20*85) = 2400 + 1700 = 4100
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(customer.Name, result.CustomerName);
        Assert.Equal(warehouse.Name, result.WarehouseName);

        var savedSo = await context.SalesOrders
            .Include(so => so.Items)
            .FirstOrDefaultAsync(so => so.Id == result.Id);
        Assert.NotNull(savedSo);
        Assert.Equal(2, savedSo.Items.Count);
    }

    [Fact]
    public async Task CreateSalesOrder_WithDeactivatedCustomer_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (customer, warehouse, productA, _) = await SeedTestDataAsync(context);

        customer.Deactivate();
        await context.SaveChangesAsync();

        var handler = new CreateSalesOrderCommandHandler(context);
        var command = new CreateSalesOrderCommand(
            customer.Id,
            warehouse.Id,
            new List<CreateSalesOrderItemInput>
            {
                new(productA.Id, Quantity: 5, UnitPrice: 200m)
            }
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("deactivated customer", ex.Message);
    }

    [Fact]
    public async Task CreateSalesOrder_WithDeactivatedWarehouse_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (customer, warehouse, productA, _) = await SeedTestDataAsync(context);

        warehouse.Deactivate();
        await context.SaveChangesAsync();

        var handler = new CreateSalesOrderCommandHandler(context);
        var command = new CreateSalesOrderCommand(
            customer.Id,
            warehouse.Id,
            new List<CreateSalesOrderItemInput>
            {
                new(productA.Id, Quantity: 5, UnitPrice: 200m)
            }
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("deactivated warehouse", ex.Message);
    }

    [Fact]
    public async Task CreateSalesOrder_WithDuplicateProduct_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (customer, warehouse, productA, _) = await SeedTestDataAsync(context);

        var handler = new CreateSalesOrderCommandHandler(context);
        var command = new CreateSalesOrderCommand(
            customer.Id,
            warehouse.Id,
            new List<CreateSalesOrderItemInput>
            {
                new(productA.Id, Quantity: 5, UnitPrice: 200m),
                new(productA.Id, Quantity: 10, UnitPrice: 190m)
            }
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfirmSalesOrder_WithSufficientStock_ShouldReserveStockAndTransitionToConfirmed()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (customer, warehouse, productA, productB) = await SeedTestDataAsync(context);

        // Stock available in warehouse: 100 units of A, 50 units of B
        var itemA = new InventoryItem(warehouse.Id, productA.Id, initialQuantity: 100);
        var itemB = new InventoryItem(warehouse.Id, productB.Id, initialQuantity: 50);
        context.InventoryItems.AddRange(itemA, itemB);

        var so = new SalesOrder("SO-CONFIRM-01", customer.Id, warehouse.Id);
        so.AddItem(productA.Id, quantity: 30, unitPrice: 250m);
        so.AddItem(productB.Id, quantity: 20, unitPrice: 90m);
        context.SalesOrders.Add(so);
        await context.SaveChangesAsync();

        var handler = new ConfirmSalesOrderCommandHandler(context, _currentUserService);

        // Act
        var result = await handler.Handle(new ConfirmSalesOrderCommand(so.Id), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(OrderStatus.Confirmed.ToString(), result.Status);
        Assert.NotNull(result.ConfirmedAt);
        Assert.Equal("test_manager", result.ConfirmedBy);

        // Verify stock reservation (Quantity remains 100, ReservedQuantity is 30, Available is 70)
        var updatedItemA = await context.InventoryItems
            .FirstOrDefaultAsync(i => i.WarehouseId == warehouse.Id && i.ProductId == productA.Id);
        Assert.NotNull(updatedItemA);
        Assert.Equal(100, updatedItemA.Quantity);
        Assert.Equal(30, updatedItemA.ReservedQuantity);
        Assert.Equal(70, updatedItemA.AvailableQuantity);

        var updatedItemB = await context.InventoryItems
            .FirstOrDefaultAsync(i => i.WarehouseId == warehouse.Id && i.ProductId == productB.Id);
        Assert.NotNull(updatedItemB);
        Assert.Equal(50, updatedItemB.Quantity);
        Assert.Equal(20, updatedItemB.ReservedQuantity);
        Assert.Equal(30, updatedItemB.AvailableQuantity);
    }

    [Fact]
    public async Task ConfirmSalesOrder_WithInsufficientAvailableStock_ShouldThrowInsufficientStockException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (customer, warehouse, productA, _) = await SeedTestDataAsync(context);

        // Only 10 units available
        var itemA = new InventoryItem(warehouse.Id, productA.Id, initialQuantity: 10);
        context.InventoryItems.Add(itemA);

        var so = new SalesOrder("SO-OVERSOLD-01", customer.Id, warehouse.Id);
        so.AddItem(productA.Id, quantity: 25, unitPrice: 250m);
        context.SalesOrders.Add(so);
        await context.SaveChangesAsync();

        var handler = new ConfirmSalesOrderCommandHandler(context, _currentUserService);

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            handler.Handle(new ConfirmSalesOrderCommand(so.Id), CancellationToken.None));

        // Verify no reservation was made
        var unchangedItemA = await context.InventoryItems
            .FirstOrDefaultAsync(i => i.WarehouseId == warehouse.Id && i.ProductId == productA.Id);
        Assert.Equal(0, unchangedItemA!.ReservedQuantity);
        Assert.Equal(10, unchangedItemA.AvailableQuantity);
    }

    [Fact]
    public async Task CompleteSalesOrder_WhenConfirmed_ShouldDeductStockAndWriteStockOutTransaction()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (customer, warehouse, productA, productB) = await SeedTestDataAsync(context);

        var itemA = new InventoryItem(warehouse.Id, productA.Id, initialQuantity: 100);
        var itemB = new InventoryItem(warehouse.Id, productB.Id, initialQuantity: 50);
        context.InventoryItems.AddRange(itemA, itemB);

        var so = new SalesOrder("SO-FULFILL-01", customer.Id, warehouse.Id);
        so.AddItem(productA.Id, quantity: 40, unitPrice: 250m);
        so.AddItem(productB.Id, quantity: 15, unitPrice: 90m);
        context.SalesOrders.Add(so);
        await context.SaveChangesAsync();

        // 1. Confirm first (allocates stock)
        var confirmHandler = new ConfirmSalesOrderCommandHandler(context, _currentUserService);
        await confirmHandler.Handle(new ConfirmSalesOrderCommand(so.Id), CancellationToken.None);

        // 2. Complete order (dispatches stock)
        var completeHandler = new CompleteSalesOrderCommandHandler(context, _currentUserService);
        var result = await completeHandler.Handle(new CompleteSalesOrderCommand(so.Id), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(OrderStatus.Completed.ToString(), result.Status);
        Assert.NotNull(result.CompletedAt);
        Assert.Equal("test_manager", result.CompletedBy);

        // Physical inventory verification:
        // Item A: was 100, 40 fulfilled -> 60 left, 0 reserved, 60 available
        var finalItemA = await context.InventoryItems
            .FirstOrDefaultAsync(i => i.WarehouseId == warehouse.Id && i.ProductId == productA.Id);
        Assert.NotNull(finalItemA);
        Assert.Equal(60, finalItemA.Quantity);
        Assert.Equal(0, finalItemA.ReservedQuantity);
        Assert.Equal(60, finalItemA.AvailableQuantity);

        // Item B: was 50, 15 fulfilled -> 35 left, 0 reserved, 35 available
        var finalItemB = await context.InventoryItems
            .FirstOrDefaultAsync(i => i.WarehouseId == warehouse.Id && i.ProductId == productB.Id);
        Assert.NotNull(finalItemB);
        Assert.Equal(35, finalItemB.Quantity);
        Assert.Equal(0, finalItemB.ReservedQuantity);
        Assert.Equal(35, finalItemB.AvailableQuantity);

        // Audit Ledger verification (StockOut records)
        var txA = await context.StockTransactions
            .FirstOrDefaultAsync(t => t.WarehouseId == warehouse.Id && t.ProductId == productA.Id);
        Assert.NotNull(txA);
        Assert.Equal(StockTransactionType.StockOut, txA.Type);
        Assert.Equal(40, txA.Quantity);
        Assert.Equal("SO-FULFILL-01", txA.ReferenceId);
        Assert.Equal("test_manager", txA.CreatedBy);

        var txB = await context.StockTransactions
            .FirstOrDefaultAsync(t => t.WarehouseId == warehouse.Id && t.ProductId == productB.Id);
        Assert.NotNull(txB);
        Assert.Equal(StockTransactionType.StockOut, txB.Type);
        Assert.Equal(15, txB.Quantity);
        Assert.Equal("SO-FULFILL-01", txB.ReferenceId);
    }

    [Fact]
    public async Task CompleteSalesOrder_WhenPendingOrCancelled_ShouldThrowInvalidOrderStateException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (customer, warehouse, productA, _) = await SeedTestDataAsync(context);

        var so = new SalesOrder("SO-COMPLETE-FAIL", customer.Id, warehouse.Id);
        so.AddItem(productA.Id, 10, 100m);
        context.SalesOrders.Add(so);
        await context.SaveChangesAsync();

        var handler = new CompleteSalesOrderCommandHandler(context, _currentUserService);

        // Act & Assert (Fulfilling an unconfirmed Pending order is strictly forbidden)
        await Assert.ThrowsAsync<InvalidOrderStateException>(() =>
            handler.Handle(new CompleteSalesOrderCommand(so.Id), CancellationToken.None));
    }

    [Fact]
    public async Task CancelSalesOrder_WhenConfirmed_ShouldReleaseReservedStock()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (customer, warehouse, productA, _) = await SeedTestDataAsync(context);

        var itemA = new InventoryItem(warehouse.Id, productA.Id, initialQuantity: 50);
        context.InventoryItems.Add(itemA);

        var so = new SalesOrder("SO-CANCEL-01", customer.Id, warehouse.Id);
        so.AddItem(productA.Id, quantity: 20, unitPrice: 250m);
        context.SalesOrders.Add(so);
        await context.SaveChangesAsync();

        // Confirm (reserves 20 units)
        var confirmHandler = new ConfirmSalesOrderCommandHandler(context, _currentUserService);
        await confirmHandler.Handle(new ConfirmSalesOrderCommand(so.Id), CancellationToken.None);

        // Cancel order
        var cancelHandler = new CancelSalesOrderCommandHandler(context);
        var result = await cancelHandler.Handle(new CancelSalesOrderCommand(so.Id), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(OrderStatus.Cancelled.ToString(), result.Status);

        // Verify reserved stock was released back to available pool
        var releasedItemA = await context.InventoryItems
            .FirstOrDefaultAsync(i => i.WarehouseId == warehouse.Id && i.ProductId == productA.Id);
        Assert.NotNull(releasedItemA);
        Assert.Equal(50, releasedItemA.Quantity);
        Assert.Equal(0, releasedItemA.ReservedQuantity); // Released!
        Assert.Equal(50, releasedItemA.AvailableQuantity); // Restored!
    }

    [Fact]
    public async Task CancelSalesOrder_WhenAlreadyCompleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (customer, warehouse, productA, _) = await SeedTestDataAsync(context);

        var itemA = new InventoryItem(warehouse.Id, productA.Id, initialQuantity: 50);
        context.InventoryItems.Add(itemA);

        var so = new SalesOrder("SO-CANCEL-FAIL", customer.Id, warehouse.Id);
        so.AddItem(productA.Id, 10, 100m);
        context.SalesOrders.Add(so);
        await context.SaveChangesAsync();

        var confirmHandler = new ConfirmSalesOrderCommandHandler(context, _currentUserService);
        await confirmHandler.Handle(new ConfirmSalesOrderCommand(so.Id), CancellationToken.None);

        var completeHandler = new CompleteSalesOrderCommandHandler(context, _currentUserService);
        await completeHandler.Handle(new CompleteSalesOrderCommand(so.Id), CancellationToken.None);

        var cancelHandler = new CancelSalesOrderCommandHandler(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cancelHandler.Handle(new CancelSalesOrderCommand(so.Id), CancellationToken.None));

        Assert.Contains("already been completed", ex.Message);
    }

    [Fact]
    public async Task GetSalesOrdersQuery_WithStatusFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (customer, warehouse, productA, _) = await SeedTestDataAsync(context);

        var so1 = new SalesOrder("SO-FILTER-01", customer.Id, warehouse.Id);
        so1.AddItem(productA.Id, 10, 100m);

        var so2 = new SalesOrder("SO-FILTER-02", customer.Id, warehouse.Id);
        so2.AddItem(productA.Id, 20, 100m);
        so2.Confirm("Manager");

        context.SalesOrders.AddRange(so1, so2);
        await context.SaveChangesAsync();

        var handler = new GetSalesOrdersQueryHandler(context);

        // Act
        var result = await handler.Handle(new GetSalesOrdersQuery(Status: OrderStatus.Confirmed), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("SO-FILTER-02", result.Items.First().OrderNumber);
    }

    [Fact]
    public async Task GetSalesOrderByIdQuery_WithValidId_ShouldReturnDetails()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (customer, warehouse, productA, _) = await SeedTestDataAsync(context);

        var so = new SalesOrder("SO-DETAIL-01", customer.Id, warehouse.Id);
        so.AddItem(productA.Id, 8, 250m);
        context.SalesOrders.Add(so);
        await context.SaveChangesAsync();

        var handler = new GetSalesOrderByIdQueryHandler(context);

        // Act
        var result = await handler.Handle(new GetSalesOrderByIdQuery(so.Id), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SO-DETAIL-01", result.OrderNumber);
        Assert.Single(result.Items);
        Assert.Equal("Industrial Sensor X1", result.Items[0].ProductName);
        Assert.Equal(2000m, result.TotalAmount);
    }
}
