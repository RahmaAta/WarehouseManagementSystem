using FluentValidation;

namespace WarehouseManagement.Application.Features.Inventory.Commands.TransferStock;

public class TransferStockCommandValidator : AbstractValidator<TransferStockCommand>
{
    public TransferStockCommandValidator()
    {
        RuleFor(x => x.FromWarehouseId)
            .GreaterThan(0).WithMessage("Valid Source Warehouse ID is required.");

        RuleFor(x => x.ToWarehouseId)
            .GreaterThan(0).WithMessage("Valid Destination Warehouse ID is required.")
            .NotEqual(x => x.FromWarehouseId).WithMessage("Source and destination warehouses cannot be the same.");

        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Valid Product ID is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity to transfer must be greater than zero.");
    }
}
