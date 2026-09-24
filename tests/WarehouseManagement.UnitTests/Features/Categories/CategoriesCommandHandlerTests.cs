using WarehouseManagement.Application.Features.Categories.Commands.CreateCategory;
using WarehouseManagement.Application.Features.Categories.Commands.DeleteCategory;
using WarehouseManagement.Application.Features.Categories.Commands.UpdateCategory;
using WarehouseManagement.Application.Features.Categories.Queries.GetCategories;
using WarehouseManagement.Application.Features.Categories.Queries.GetCategoryById;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.UnitTests.Common;

namespace WarehouseManagement.UnitTests.Features.Categories;

public class CategoriesCommandHandlerTests
{
    [Fact]
    public async Task CreateCategory_WithValidName_ShouldPersistAndReturnDto()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var handler = new CreateCategoryCommandHandler(context);
        var command = new CreateCategoryCommand("Electronics", "Electronic components and devices");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Electronics", result.Name);
        Assert.Equal("Electronic components and devices", result.Description);
        Assert.True(result.IsActive);
        Assert.Equal(0, result.ProductsCount);

        var saved = await context.Categories.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("Electronics", saved.Name);
    }

    [Fact]
    public async Task CreateCategory_WithDuplicateName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.Categories.Add(new Category("Furniture", "Office and warehouse furniture"));
        await context.SaveChangesAsync();

        var handler = new CreateCategoryCommandHandler(context);
        var command = new CreateCategoryCommand("furniture"); // Case-insensitive duplicate test

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task UpdateCategory_WithValidData_ShouldUpdateAndReturnDto()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Raw Materials", "Original description");
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var handler = new UpdateCategoryCommandHandler(context);
        var command = new UpdateCategoryCommand(category.Id, "Primary Raw Materials", "Updated description");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("Primary Raw Materials", result.Name);
        Assert.Equal("Updated description", result.Description);

        var updated = await context.Categories.FindAsync(category.Id);
        Assert.Equal("Primary Raw Materials", updated!.Name);
    }

    [Fact]
    public async Task UpdateCategory_WithDuplicateNameOfAnotherCategory_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.Categories.AddRange(
            new Category("Category A"),
            new Category("Category B")
        );
        await context.SaveChangesAsync();

        var handler = new UpdateCategoryCommandHandler(context);
        var command = new UpdateCategoryCommand(2, "category a", null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteCategory_WhenNoProductsAssigned_ShouldRemoveCategory()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Empty Category");
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var handler = new DeleteCategoryCommandHandler(context);
        var command = new DeleteCategoryCommand(category.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        var deleted = await context.Categories.FindAsync(category.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteCategory_WhenProductsAssigned_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category("Category In Use");
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product("Pallet Jack", "TOOL-PJ-01", 350.00m, 5, category.Id);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var handler = new DeleteCategoryCommandHandler(context);
        var command = new DeleteCategoryCommand(category.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("active or historical products are assigned", ex.Message);
    }

    [Fact]
    public async Task GetCategoriesQuery_ShouldReturnCategoriesWithProductCounts()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var cat1 = new Category("Hardware");
        var cat2 = new Category("Packaging");
        context.Categories.AddRange(cat1, cat2);
        await context.SaveChangesAsync();

        context.Products.Add(new Product("Nails 2-inch", "HDW-N2", 15.00m, 100, cat1.Id));
        context.Products.Add(new Product("Screws M4", "HDW-SM4", 25.00m, 100, cat1.Id));
        await context.SaveChangesAsync();

        var handler = new GetCategoriesQueryHandler(context);
        var query = new GetCategoriesQuery(IncludeInactive: false);

        // Act
        var list = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, list.Count);
        var hardware = list.First(c => c.Name == "Hardware");
        Assert.Equal(2, hardware.ProductsCount);

        var packaging = list.First(c => c.Name == "Packaging");
        Assert.Equal(0, packaging.ProductsCount);
    }
}
