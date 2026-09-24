using FluentValidation;

namespace WarehouseManagement.Application.Features.Inventory.Commands.AdjustStock;

public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("Valid Warehouse ID is required.");

        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Valid Product ID is required.");

        RuleFor(x => x.ActualCountedQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Actual counted quantity cannot be negative.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Adjustment reason is mandatory for audit logging.")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}
