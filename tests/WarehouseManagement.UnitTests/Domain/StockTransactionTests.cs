using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Domain.Enums;
using WarehouseManagement.Domain.Exceptions;
using Xunit;

namespace WarehouseManagement.UnitTests.Domain;

public class StockTransactionTests
{
    [Fact]
    public void CreateTransfer_ToSameWarehouse_ShouldThrowSameWarehouseTransferException()
    {
        // Act & Assert
        var ex = Assert.Throws<SameWarehouseTransferException>(() =>
            StockTransaction.CreateTransfer(productId: 1, fromWarehouseId: 5, toWarehouseId: 5, quantity: 10, referenceId: "TR-001", notes: "Test"));

        Assert.Equal(5, ex.WarehouseId);
    }

    [Fact]
    public void CreateStockIn_WithValidParameters_ShouldInstantiateProperly()
    {
        // Act
        var tx = StockTransaction.CreateStockIn(productId: 2, warehouseId: 1, quantity: 50, referenceId: "PO-501", notes: "Initial intake", createdBy: "admin");

        // Assert
        Assert.Equal(StockTransactionType.StockIn, tx.Type);
        Assert.Equal(2, tx.ProductId);
        Assert.Equal(1, tx.WarehouseId);
        Assert.Equal(50, tx.Quantity);
        Assert.Equal("PO-501", tx.ReferenceId);
        Assert.Equal("admin", tx.CreatedBy);
    }
}

public class ProductTests
{
    [Fact]
    public void SetPrice_WithNegativeValue_ShouldThrowNegativePriceException()
    {
        // Arrange
        var product = new Product("Widget", "WDG-001", price: 10.0m, minimumStockLevel: 5, categoryId: 1);

        // Act & Assert
        var ex = Assert.Throws<NegativePriceException>(() => product.SetPrice(-5m));
        Assert.Equal(-5m, ex.AttemptedPrice);
    }
}
