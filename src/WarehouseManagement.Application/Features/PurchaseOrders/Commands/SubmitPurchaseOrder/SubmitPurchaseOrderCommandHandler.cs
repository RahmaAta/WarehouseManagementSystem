using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Application.Features.PurchaseOrders.DTOs;

namespace WarehouseManagement.Application.Features.PurchaseOrders.Commands.SubmitPurchaseOrder;

public class SubmitPurchaseOrderCommandHandler : IRequestHandler<SubmitPurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IApplicationDbContext _context;

    public SubmitPurchaseOrderCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PurchaseOrderDto> Handle(SubmitPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Warehouse)
            .Include(po => po.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(po => po.Id == request.Id, cancellationToken);

        if (purchaseOrder == null)
        {
            throw new KeyNotFoundException($"Purchase order with ID {request.Id} was not found.");
        }

        // Domain method validates that status is Draft and items are present
        purchaseOrder.SubmitForApproval();

        await _context.SaveChangesAsync(cancellationToken);

        var itemDtos = purchaseOrder.Items.Select(i => new PurchaseOrderItemDto(
            i.Id,
            i.ProductId,
            i.Product?.Name ?? string.Empty,
            i.Product?.SKU ?? string.Empty,
            i.Quantity,
            i.ReceivedQuantity,
            i.UnitPrice,
            i.TotalPrice
        )).ToList();

        return new PurchaseOrderDto(
            purchaseOrder.Id,
            purchaseOrder.OrderNumber,
            purchaseOrder.SupplierId,
            purchaseOrder.Supplier?.Name ?? string.Empty,
            purchaseOrder.WarehouseId,
            purchaseOrder.Warehouse?.Name ?? string.Empty,
            purchaseOrder.Status.ToString(),
            purchaseOrder.TotalAmount,
            purchaseOrder.ApprovedBy,
            purchaseOrder.ApprovedAt,
            purchaseOrder.ReceivedBy,
            purchaseOrder.ReceivedAt,
            purchaseOrder.CreatedAt,
            itemDtos
        );
    }
}
