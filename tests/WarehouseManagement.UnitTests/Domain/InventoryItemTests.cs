using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Domain.Exceptions;
using Xunit;

namespace WarehouseManagement.UnitTests.Domain;

public class InventoryItemTests
{
    [Fact]
    public void AddStock_WithPositiveQuantity_ShouldIncreaseQuantity()
    {
        // Arrange
        var item = new InventoryItem(warehouseId: 1, productId: 10, initialQuantity: 20);

        // Act
        item.AddStock(15);

        // Assert
        Assert.Equal(35, item.Quantity);
        Assert.Equal(35, item.AvailableQuantity);
    }

    [Fact]
    public void RemoveStock_WhenStockIsSufficient_ShouldDecreaseQuantity()
    {
        // Arrange
        var item = new InventoryItem(warehouseId: 1, productId: 10, initialQuantity: 50);

        // Act
        item.RemoveStock(20);

        // Assert
        Assert.Equal(30, item.Quantity);
        Assert.Equal(30, item.AvailableQuantity);
    }

    [Fact]
    public void RemoveStock_WhenQuantityExceedsAvailable_ShouldThrowInsufficientStockException()
    {
        // Arrange
        var item = new InventoryItem(warehouseId: 1, productId: 10, initialQuantity: 10);

        // Act & Assert
        var ex = Assert.Throws<InsufficientStockException>(() => item.RemoveStock(15));
        Assert.Equal(10, ex.ProductId);
        Assert.Equal(1, ex.WarehouseId);
        Assert.Equal(15, ex.RequestedQuantity);
        Assert.Equal(10, ex.AvailableQuantity);
    }

    [Fact]
    public void ReserveStock_WhenAvailableStockSufficient_ShouldIncreaseReservedQuantity()
    {
        // Arrange
        var item = new InventoryItem(warehouseId: 1, productId: 10, initialQuantity: 25);

        // Act
        item.ReserveStock(10);

        // Assert
        Assert.Equal(25, item.Quantity);
        Assert.Equal(10, item.ReservedQuantity);
        Assert.Equal(15, item.AvailableQuantity);
    }

    [Fact]
    public void ReserveStock_WhenRequestedExceedsAvailable_ShouldThrowInsufficientStockException()
    {
        // Arrange
        var item = new InventoryItem(warehouseId: 1, productId: 10, initialQuantity: 10);
        item.ReserveStock(6); // Available is now 4

        // Act & Assert
        Assert.Throws<InsufficientStockException>(() => item.ReserveStock(5));
    }
}
