using WarehouseManagement.Application.Features.Suppliers.Commands.ActivateSupplier;
using WarehouseManagement.Application.Features.Suppliers.Commands.CreateSupplier;
using WarehouseManagement.Application.Features.Suppliers.Commands.DeleteSupplier;
using WarehouseManagement.Application.Features.Suppliers.Commands.UpdateSupplier;
using WarehouseManagement.Application.Features.Suppliers.Queries.GetSupplierById;
using WarehouseManagement.Application.Features.Suppliers.Queries.GetSuppliers;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.UnitTests.Common;

namespace WarehouseManagement.UnitTests.Features.Suppliers;

public class SuppliersCommandHandlerTests
{
    [Fact]
    public async Task CreateSupplier_WithValidData_ShouldSucceed()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var handler = new CreateSupplierCommandHandler(context);
        var command = new CreateSupplierCommand(
            Name: "Global Steel Supplies",
            Email: "sales@globalsteel.com",
            PhoneNumber: "+201001234567",
            ContactPerson: "Ahmed Hassan",
            Address: "10th of Ramadan Zone B");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Global Steel Supplies", result.Name);
        Assert.Equal("sales@globalsteel.com", result.Email);
        Assert.True(result.IsActive);
        Assert.Equal(0, result.TotalPurchaseOrdersCount);

        var saved = await context.Suppliers.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("Global Steel Supplies", saved.Name);
    }

    [Fact]
    public async Task CreateSupplier_WithDuplicateEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.Suppliers.Add(new Supplier("Egyptian Plastics Corp", "info@egyplastics.com", "+201011112222"));
        await context.SaveChangesAsync();

        var handler = new CreateSupplierCommandHandler(context);
        var command = new CreateSupplierCommand("Delta Plastics", "INFO@EGYPLASTICS.COM", "+201033334444");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task UpdateSupplier_WithValidData_ShouldUpdate()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var supplier = new Supplier("Apex Hardware", "contact@apex.com", "+201055556666");
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        var handler = new UpdateSupplierCommandHandler(context);
        var command = new UpdateSupplierCommand(
            supplier.Id,
            "Apex Hardware International",
            "support@apex.com",
            "+201055557777",
            ContactPerson: "Mona Adel",
            Address: "Nasr City Cairo");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("Apex Hardware International", result.Name);
        Assert.Equal("support@apex.com", result.Email);
        Assert.Equal("Mona Adel", result.ContactPerson);

        var updated = await context.Suppliers.FindAsync(supplier.Id);
        Assert.Equal("Apex Hardware International", updated!.Name);
    }

    [Fact]
    public async Task UpdateSupplier_WithDuplicateEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.Suppliers.AddRange(
            new Supplier("Vendor A", "vendorA@supplies.com", "+201000000001"),
            new Supplier("Vendor B", "vendorB@supplies.com", "+201000000002")
        );
        await context.SaveChangesAsync();

        var handler = new UpdateSupplierCommandHandler(context);
        var command = new UpdateSupplierCommand(2, "Vendor B Upgraded", "vendorA@supplies.com", "+201000000002");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("Another supplier already exists", ex.Message);
    }

    [Fact]
    public async Task DeleteSupplier_ShouldDeactivateSupplier()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var supplier = new Supplier("Outdated Vendor", "old@vendor.com", "+201099990000");
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        var handler = new DeleteSupplierCommandHandler(context);
        var command = new DeleteSupplierCommand(supplier.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        var reloaded = await context.Suppliers.FindAsync(supplier.Id);
        Assert.NotNull(reloaded);
        Assert.False(reloaded.IsActive); // Soft-deleted
    }

    [Fact]
    public async Task ActivateSupplier_WhenDeactivated_ShouldReactivate()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var supplier = new Supplier("Seasonal Vendor", "seasonal@vendor.com", "+201088887777");
        supplier.Deactivate();
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        var handler = new ActivateSupplierCommandHandler(context);
        var command = new ActivateSupplierCommand(supplier.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        var reloaded = await context.Suppliers.FindAsync(supplier.Id);
        Assert.NotNull(reloaded);
        Assert.True(reloaded.IsActive);
    }

    [Fact]
    public async Task GetSuppliersQuery_WithSearchAndPagination_ShouldReturnFilteredList()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.Suppliers.AddRange(
            new Supplier("Nile Packing Materials", "contact@nilepack.com", "+20101", contactPerson: "Tarek"),
            new Supplier("Nile Chemicals", "sales@nilechem.com", "+20102", contactPerson: "Yasser"),
            new Supplier("Delta Logistics", "info@deltalog.com", "+20103", contactPerson: "Hossam")
        );
        await context.SaveChangesAsync();

        var handler = new GetSuppliersQueryHandler(context);
        var query = new GetSuppliersQuery(PageNumber: 1, PageSize: 10, SearchTerm: "Nile", IncludeInactive: false);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, s => Assert.Contains("Nile", s.Name));
    }
}
