using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Domain.Enums;
using WarehouseManagement.Domain.Exceptions;
using Xunit;

namespace WarehouseManagement.UnitTests.Domain;

public class PurchaseOrderTests
{
    [Fact]
    public void MarkAsReceived_WhenOrderIsNotApproved_ShouldThrowInvalidOrderStateException()
    {
        // Arrange
        var po = new PurchaseOrder("PO-1001", supplierId: 1, warehouseId: 1);
        po.AddItem(productId: 5, quantity: 10, unitPrice: 25.50m);

        // Current status is Draft. Attempting to receive must fail!
        // Act & Assert
        var ex = Assert.Throws<InvalidOrderStateException>(() => po.MarkAsReceived("staff_user"));
        Assert.Contains("Cannot transition PurchaseOrder", ex.Message);
    }

    [Fact]
    public void ApproveAndReceive_FollowingCorrectLifecycle_ShouldSucceed()
    {
        // Arrange
        var po = new PurchaseOrder("PO-1002", supplierId: 1, warehouseId: 1);
        po.AddItem(productId: 5, quantity: 10, unitPrice: 20m);
        po.SubmitForApproval();

        // Act
        po.Approve("manager_user");
        po.MarkAsReceived("staff_user");

        // Assert
        Assert.Equal(PurchaseOrderStatus.Received, po.Status);
        Assert.NotNull(po.ApprovedAt);
        Assert.Equal("manager_user", po.ApprovedBy);
        Assert.NotNull(po.ReceivedAt);
        Assert.Equal("staff_user", po.ReceivedBy);
    }

    [Fact]
    public void AddItem_ShouldRecalculateTotalAmount()
    {
        // Arrange
        var po = new PurchaseOrder("PO-1003", supplierId: 1, warehouseId: 1);

        // Act
        po.AddItem(productId: 1, quantity: 2, unitPrice: 10m); // 20
        po.AddItem(productId: 2, quantity: 3, unitPrice: 15m); // 45

        // Assert
        Assert.Equal(65m, po.TotalAmount);
    }
}
