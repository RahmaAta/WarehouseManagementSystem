using WarehouseManagement.Application.Features.Warehouses.Commands.ActivateWarehouse;
using WarehouseManagement.Application.Features.Warehouses.Commands.CreateWarehouse;
using WarehouseManagement.Application.Features.Warehouses.Commands.DeleteWarehouse;
using WarehouseManagement.Application.Features.Warehouses.Commands.UpdateWarehouse;
using WarehouseManagement.Application.Features.Warehouses.Queries.GetWarehouseById;
using WarehouseManagement.Application.Features.Warehouses.Queries.GetWarehouses;
using WarehouseManagement.Application.Features.Warehouses.Queries.GetWarehouseStock;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.UnitTests.Common;

namespace WarehouseManagement.UnitTests.Features.Warehouses;

public class WarehousesCommandHandlerTests
{
    [Fact]
    public async Task CreateWarehouse_WithValidData_ShouldSucceed()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var handler = new CreateWarehouseCommandHandler(context);
        var command = new CreateWarehouseCommand("Cairo Central Distribution", "10th of Ramadan Industrial Zone");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Cairo Central Distribution", result.Name);
        Assert.Equal("10th of Ramadan Industrial Zone", result.Location);
        Assert.True(result.IsActive);
        Assert.Equal(0, result.TotalDistinctProducts);
        Assert.Equal(0, result.TotalStockUnits);

        var saved = await context.Warehouses.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("Cairo Central Distribution", saved.Name);
    }

    [Fact]
    public async Task CreateWarehouse_WithDuplicateName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.Warehouses.Add(new Warehouse("Alexandria Hub", "Borg El Arab"));
        await context.SaveChangesAsync();

        var handler = new CreateWarehouseCommandHandler(context);
        var command = new CreateWarehouseCommand("alexandria hub", "New Location");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task UpdateWarehouse_WithValidData_ShouldUpdateAndReturnDto()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var warehouse = new Warehouse("Giza Facility", "6th of October");
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var handler = new UpdateWarehouseCommandHandler(context);
        var command = new UpdateWarehouseCommand(warehouse.Id, "Giza West Logistics Park", "Zone 3, 6th of October");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("Giza West Logistics Park", result.Name);
        Assert.Equal("Zone 3, 6th of October", result.Location);

        var updated = await context.Warehouses.FindAsync(warehouse.Id);
        Assert.Equal("Giza West Logistics Park", updated!.Name);
    }

    [Fact]
    public async Task UpdateWarehouse_WithDuplicateNameOfAnotherWarehouse_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.Warehouses.AddRange(
            new Warehouse("Facility Alpha", "Location A"),
            new Warehouse("Facility Beta", "Location B")
        );
        await context.SaveChangesAsync();

        var handler = new UpdateWarehouseCommandHandler(context);
        var command = new UpdateWarehouseCommand(2, "facility alpha", "Location B Updated");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteWarehouse_WhenEmpty_ShouldDeactivateWarehouse()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var warehouse = new Warehouse("Empty Facility", "Desert Road");
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var handler = new DeleteWarehouseCommandHandler(context);
        var command = new DeleteWarehouseCommand(warehouse.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        var reloaded = await context.Warehouses.FindAsync(warehouse.Id);
        Assert.NotNull(reloaded);
        Assert.False(reloaded.IsActive);
    }

    [Fact]
    public async Task DeleteWarehouse_WhenStoresStock_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Raw Materials");
        context.Categories.Add(category);

        var product = new Product("Copper Wire Reel", "RAW-CPR-01", 85.00m, 10, category.Id);
        context.Products.Add(product);

        var warehouse = new Warehouse("Active Stock Facility", "Industrial Area");
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var inventoryItem = new InventoryItem(warehouse.Id, product.Id, initialQuantity: 120);
        context.InventoryItems.Add(inventoryItem);
        await context.SaveChangesAsync();

        var handler = new DeleteWarehouseCommandHandler(context);
        var command = new DeleteWarehouseCommand(warehouse.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("currently stores 120 units", ex.Message);
    }

    [Fact]
    public async Task ActivateWarehouse_WhenDeactivated_ShouldReactivateWarehouse()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var warehouse = new Warehouse("Season Storage", "Suez Road");
        warehouse.Deactivate();
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var handler = new ActivateWarehouseCommandHandler(context);
        var command = new ActivateWarehouseCommand(warehouse.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        var reloaded = await context.Warehouses.FindAsync(warehouse.Id);
        Assert.NotNull(reloaded);
        Assert.True(reloaded.IsActive);
    }

    [Fact]
    public async Task GetWarehousesQuery_ShouldReturnWarehousesWithTotals()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("General");
        context.Categories.Add(category);

        var product1 = new Product("Pallet Wood", "PLT-WD", 25m, 10, category.Id);
        var product2 = new Product("Steel Strapping", "ST-STP", 40m, 10, category.Id);
        context.Products.AddRange(product1, product2);

        var warehouse = new Warehouse("Delta Logistics", "Tanta");
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        context.InventoryItems.AddRange(
            new InventoryItem(warehouse.Id, product1.Id, initialQuantity: 50),
            new InventoryItem(warehouse.Id, product2.Id, initialQuantity: 75)
        );
        await context.SaveChangesAsync();

        var handler = new GetWarehousesQueryHandler(context);
        var query = new GetWarehousesQuery(IncludeInactive: false);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var item = result.First();
        Assert.Equal("Delta Logistics", item.Name);
        Assert.Equal(2, item.TotalDistinctProducts);
        Assert.Equal(125, item.TotalStockUnits);
    }

    [Fact]
    public async Task GetWarehouseStockQuery_ShouldReturnPaginatedStock()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Packaging");
        context.Categories.Add(category);

        var product = new Product("Corrugated Box XL", "BOX-XL-01", 4.5m, 50, category.Id);
        context.Products.Add(product);

        var warehouse = new Warehouse("Suez Logistics Depot", "Suez Canal Zone");
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        context.InventoryItems.Add(new InventoryItem(warehouse.Id, product.Id, initialQuantity: 300));
        await context.SaveChangesAsync();

        var handler = new GetWarehouseStockQueryHandler(context);
        var query = new GetWarehouseStockQuery(warehouse.Id, PageNumber: 1, PageSize: 10, SearchTerm: "Corrugated");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Corrugated Box XL", result.Items.First().ProductName);
        Assert.Equal(300, result.Items.First().Quantity);
        Assert.Equal(300, result.Items.First().AvailableQuantity);
    }
}
