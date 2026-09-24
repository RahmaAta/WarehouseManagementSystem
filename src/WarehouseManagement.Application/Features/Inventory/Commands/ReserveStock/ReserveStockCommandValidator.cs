using FluentValidation;

namespace WarehouseManagement.Application.Features.Inventory.Commands.ReserveStock;

public class ReserveStockCommandValidator : AbstractValidator<ReserveStockCommand>
{
    public ReserveStockCommandValidator()
    {
        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("Valid Warehouse ID is required.");

        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Valid Product ID is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity to reserve must be greater than zero.");
    }
}
