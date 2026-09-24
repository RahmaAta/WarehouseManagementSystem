using FluentValidation.TestHelper;
using WarehouseManagement.Application.Features.Auth.Commands.RegisterUser;
using WarehouseManagement.Application.Features.Inventory.Commands.AdjustStock;
using WarehouseManagement.Application.Features.Inventory.Commands.TransferStock;
using WarehouseManagement.Application.Features.Products.Commands.CreateProduct;
using WarehouseManagement.Application.Features.SalesOrders.Commands.CreateSalesOrder;

namespace WarehouseManagement.UnitTests.Features.Validators;

public class CommandValidatorsTests
{
    [Fact]
    public void CreateProductCommandValidator_WithInvalidInputs_ShouldHaveValidationErrors()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(
            Name: "",
            SKU: "INVALID SKU WITH SPACES$$",
            Price: -50m,
            MinimumStockLevel: -5,
            CategoryId: 0
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.SKU);
        result.ShouldHaveValidationErrorFor(x => x.Price);
        result.ShouldHaveValidationErrorFor(x => x.MinimumStockLevel);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void CreateProductCommandValidator_WithValidInputs_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(
            Name: "Laser Scanner Pro",
            SKU: "LSR-SCN-001",
            Price: 499.99m,
            MinimumStockLevel: 10,
            CategoryId: 1
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateSalesOrderCommandValidator_WithEmptyItems_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new CreateSalesOrderCommandValidator();
        var command = new CreateSalesOrderCommand(
            CustomerId: 1,
            WarehouseId: 1,
            Items: new List<CreateSalesOrderItemInput>()
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void CreateSalesOrderCommandValidator_WithInvalidLineItem_ShouldHaveValidationErrors()
    {
        // Arrange
        var validator = new CreateSalesOrderCommandValidator();
        var command = new CreateSalesOrderCommand(
            CustomerId: 0,
            WarehouseId: 0,
            Items: new List<CreateSalesOrderItemInput>
            {
                new(ProductId: 0, Quantity: 0, UnitPrice: -10m)
            }
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
        result.ShouldHaveValidationErrorFor(x => x.WarehouseId);
        result.ShouldHaveValidationErrorFor("Items[0].ProductId");
        result.ShouldHaveValidationErrorFor("Items[0].Quantity");
        result.ShouldHaveValidationErrorFor("Items[0].UnitPrice");
    }

    [Fact]
    public void TransferStockCommandValidator_WithSameSourceAndDestination_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new TransferStockCommandValidator();
        var command = new TransferStockCommand(
            FromWarehouseId: 1,
            ToWarehouseId: 1, // Same!
            ProductId: 5,
            Quantity: 10
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ToWarehouseId);
    }

    [Fact]
    public void AdjustStockCommandValidator_WithEmptyReason_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new AdjustStockCommandValidator();
        var command = new AdjustStockCommand(
            WarehouseId: 1,
            ProductId: 2,
            ActualCountedQuantity: 25,
            Reason: "" // Empty!
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void RegisterUserCommandValidator_WithInvalidEmailAndPassword_ShouldHaveValidationErrors()
    {
        // Arrange
        var validator = new RegisterUserCommandValidator();
        var command = new RegisterUserCommand(
            Username: "u$", // too short & special char
            Email: "not-an-email",
            Password: "123", // too short
            FullName: "",
            RoleId: 0
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username);
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Password);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
        result.ShouldHaveValidationErrorFor(x => x.RoleId);
    }
}
