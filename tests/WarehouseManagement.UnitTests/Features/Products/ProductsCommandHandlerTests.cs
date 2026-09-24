using WarehouseManagement.Application.Features.Products.Commands.ActivateProduct;
using WarehouseManagement.Application.Features.Products.Commands.CreateProduct;
using WarehouseManagement.Application.Features.Products.Commands.DeleteProduct;
using WarehouseManagement.Application.Features.Products.Commands.UpdateProduct;
using WarehouseManagement.Application.Features.Products.Queries.GetProductById;
using WarehouseManagement.Application.Features.Products.Queries.GetProductBySku;
using WarehouseManagement.Application.Features.Products.Queries.GetProducts;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.UnitTests.Common;

namespace WarehouseManagement.UnitTests.Features.Products;

public class ProductsCommandHandlerTests
{
    [Fact]
    public async Task CreateProduct_WithValidData_ShouldPersistAndReturnDto()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Electronics");
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var handler = new CreateProductCommandHandler(context);
        var command = new CreateProductCommand(
            "Laser Scanner Wireless",
            "SCAN-WL-01",
            149.99m,
            10,
            category.Id,
            "Industrial 2D wireless barcode scanner");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Laser Scanner Wireless", result.Name);
        Assert.Equal("SCAN-WL-01", result.SKU);
        Assert.Equal(149.99m, result.Price);
        Assert.Equal(10, result.MinimumStockLevel);
        Assert.Equal("Electronics", result.CategoryName);
        Assert.True(result.IsActive);
        Assert.Equal(0, result.TotalAvailableStock);

        var saved = await context.Products.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("SCAN-WL-01", saved.SKU);
    }

    [Fact]
    public async Task CreateProduct_WithNonExistentCategory_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var handler = new CreateProductCommandHandler(context);
        var command = new CreateProductCommand("Widget", "WDG-01", 10m, 5, 9999);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateProduct_WithDuplicateSku_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Tools");
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var existing = new Product("Wrench 10mm", "WR-10MM", 12.50m, 20, category.Id);
        context.Products.Add(existing);
        await context.SaveChangesAsync();

        var handler = new CreateProductCommandHandler(context);
        var command = new CreateProductCommand("Adjustable Wrench", "wr-10mm", 15.00m, 10, category.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task UpdateProduct_WithDuplicateSkuOfAnotherProduct_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Packaging");
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product1 = new Product("Box Small", "BOX-SM", 1.50m, 100, category.Id);
        var product2 = new Product("Box Large", "BOX-LG", 3.50m, 50, category.Id);
        context.Products.AddRange(product1, product2);
        await context.SaveChangesAsync();

        var handler = new UpdateProductCommandHandler(context);
        var command = new UpdateProductCommand(product2.Id, "Box Large Upgraded", "BOX-SM", 4.00m, 50, category.Id, null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("Another product already exists", ex.Message);
    }

    [Fact]
    public async Task DeleteProduct_WithZeroInventoryOnHand_ShouldDeactivateProduct()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Discontinued");
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product("Legacy Tape", "TAPE-LEG", 2.00m, 10, category.Id);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var handler = new DeleteProductCommandHandler(context);
        var command = new DeleteProductCommand(product.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        var reloaded = await context.Products.FindAsync(product.Id);
        Assert.NotNull(reloaded);
        Assert.False(reloaded.IsActive); // Soft-deleted / deactivated
    }

    [Fact]
    public async Task DeleteProduct_WithPhysicalInventoryOnHand_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Apparel");
        context.Categories.Add(category);

        var warehouse = new Warehouse("Main Logistics Hub", "123 Port Road");
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var product = new Product("Safety Vest", "VEST-SF-01", 18.00m, 25, category.Id);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Add 50 units in stock
        var item = new InventoryItem(warehouse.Id, product.Id, initialQuantity: 50);
        context.InventoryItems.Add(item);
        await context.SaveChangesAsync();

        var handler = new DeleteProductCommandHandler(context);
        var command = new DeleteProductCommand(product.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("units currently on hand", ex.Message);
    }

    [Fact]
    public async Task ActivateProduct_WhenDeactivated_ShouldReactivateProduct()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Seasonal");
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product("Thermal Gloves", "GLV-TH-01", 12.00m, 50, category.Id);
        product.Deactivate();
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var handler = new ActivateProductCommandHandler(context);
        var command = new ActivateProductCommand(product.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        var reloaded = await context.Products.FindAsync(product.Id);
        Assert.NotNull(reloaded);
        Assert.True(reloaded.IsActive);
    }

    [Fact]
    public async Task GetProductBySkuQuery_WithValidSku_ShouldReturnProduct()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Hardware");
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product("Steel Bolt M8", "BLT-M8-100", 0.85m, 500, category.Id);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var handler = new GetProductBySkuQueryHandler(context);
        var query = new GetProductBySkuQuery("blt-m8-100"); // case-insensitive match

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Steel Bolt M8", result.Name);
        Assert.Equal("BLT-M8-100", result.SKU);
        Assert.Equal("Hardware", result.CategoryName);
    }

    [Fact]
    public async Task GetProductsQuery_WithSearchAndCategoryFilter_ShouldReturnFilteredPaginatedResult()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var cat1 = new Category("Office");
        var cat2 = new Category("Warehouse Tools");
        context.Categories.AddRange(cat1, cat2);
        await context.SaveChangesAsync();

        context.Products.AddRange(
            new Product("Stapler Heavy Duty", "OFF-STP-01", 15.00m, 10, cat1.Id),
            new Product("Staple Pins Box", "OFF-PIN-01", 2.00m, 100, cat1.Id),
            new Product("Packing Tape Gun", "TOOL-TG-01", 19.50m, 15, cat2.Id)
        );
        await context.SaveChangesAsync();

        var handler = new GetProductsQueryHandler(context);
        var query = new GetProductsQuery(
            PageNumber: 1,
            PageSize: 10,
            CategoryId: cat1.Id,
            SearchTerm: "Stapler",
            OnlyActive: true);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Stapler Heavy Duty", result.Items.First().Name);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }
}
