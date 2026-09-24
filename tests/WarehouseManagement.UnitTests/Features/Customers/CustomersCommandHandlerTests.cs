using WarehouseManagement.Application.Features.Customers.Commands.ActivateCustomer;
using WarehouseManagement.Application.Features.Customers.Commands.CreateCustomer;
using WarehouseManagement.Application.Features.Customers.Commands.DeleteCustomer;
using WarehouseManagement.Application.Features.Customers.Commands.UpdateCustomer;
using WarehouseManagement.Application.Features.Customers.Queries.GetCustomerById;
using WarehouseManagement.Application.Features.Customers.Queries.GetCustomers;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.UnitTests.Common;

namespace WarehouseManagement.UnitTests.Features.Customers;

public class CustomersCommandHandlerTests
{
    [Fact]
    public async Task CreateCustomer_WithValidData_ShouldSucceed()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var handler = new CreateCustomerCommandHandler(context);
        var command = new CreateCustomerCommand(
            Name: "El Sewedy Electrics",
            Email: "orders@elsewedy.com",
            PhoneNumber: "+201012345678",
            Address: "5th Settlement, New Cairo");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("El Sewedy Electrics", result.Name);
        Assert.Equal("orders@elsewedy.com", result.Email);
        Assert.True(result.IsActive);
        Assert.Equal(0, result.TotalSalesOrdersCount);

        var saved = await context.Customers.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("El Sewedy Electrics", saved.Name);
    }

    [Fact]
    public async Task CreateCustomer_WithDuplicateEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.Customers.Add(new Customer("Existing Corp", "corp@client.com", "+201111111111"));
        await context.SaveChangesAsync();

        var handler = new CreateCustomerCommandHandler(context);
        var command = new CreateCustomerCommand("New Corp", "CORP@CLIENT.COM", "+201222222222");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task UpdateCustomer_WithValidData_ShouldUpdate()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var customer = new Customer("Modern Retail Stores", "contact@modernretail.com", "+201000000010");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var handler = new UpdateCustomerCommandHandler(context);
        var command = new UpdateCustomerCommand(
            customer.Id,
            "Modern Hypermarkets Group",
            "hq@modernretail.com",
            "+201000000020",
            Address: "Sheikh Zayed Commercial Strip");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("Modern Hypermarkets Group", result.Name);
        Assert.Equal("hq@modernretail.com", result.Email);
        Assert.Equal("Sheikh Zayed Commercial Strip", result.Address);

        var updated = await context.Customers.FindAsync(customer.Id);
        Assert.Equal("Modern Hypermarkets Group", updated!.Name);
    }

    [Fact]
    public async Task UpdateCustomer_WithDuplicateEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.Customers.AddRange(
            new Customer("Customer 1", "cust1@mail.com", "+20101"),
            new Customer("Customer 2", "cust2@mail.com", "+20102")
        );
        await context.SaveChangesAsync();

        var handler = new UpdateCustomerCommandHandler(context);
        var command = new UpdateCustomerCommand(2, "Customer 2 Updated", "cust1@mail.com", "+20102");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("Another customer already exists", ex.Message);
    }

    [Fact]
    public async Task DeleteCustomer_ShouldDeactivateCustomer()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var customer = new Customer("Closed Client", "closed@client.com", "+201099999999");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var handler = new DeleteCustomerCommandHandler(context);
        var command = new DeleteCustomerCommand(customer.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        var reloaded = await context.Customers.FindAsync(customer.Id);
        Assert.NotNull(reloaded);
        Assert.False(reloaded.IsActive); // Soft-deleted
    }

    [Fact]
    public async Task ActivateCustomer_WhenDeactivated_ShouldReactivate()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var customer = new Customer("Dormant Client", "dormant@client.com", "+201088888888");
        customer.Deactivate();
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var handler = new ActivateCustomerCommandHandler(context);
        var command = new ActivateCustomerCommand(customer.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        var reloaded = await context.Customers.FindAsync(customer.Id);
        Assert.NotNull(reloaded);
        Assert.True(reloaded.IsActive);
    }

    [Fact]
    public async Task GetCustomersQuery_WithSearchAndPagination_ShouldReturnFilteredList()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.Customers.AddRange(
            new Customer("Al-Ahram Logistics", "logistics@ahram.com", "+201001"),
            new Customer("Al-Ahram Industrial", "industry@ahram.com", "+201002"),
            new Customer("Cairo Trading Co", "trading@cairo.com", "+201003")
        );
        await context.SaveChangesAsync();

        var handler = new GetCustomersQueryHandler(context);
        var query = new GetCustomersQuery(PageNumber: 1, PageSize: 10, SearchTerm: "Ahram", IncludeInactive: false);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, c => Assert.Contains("Ahram", c.Name));
    }
}
