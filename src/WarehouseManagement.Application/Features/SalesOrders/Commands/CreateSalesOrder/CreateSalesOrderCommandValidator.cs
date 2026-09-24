using FluentValidation;

namespace WarehouseManagement.Application.Features.SalesOrders.Commands.CreateSalesOrder;

public class CreateSalesOrderCommandValidator : AbstractValidator<CreateSalesOrderCommand>
{
    public CreateSalesOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Valid Customer ID is required.");

        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("Valid Warehouse ID is required.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Sales order must contain at least one item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .GreaterThan(0).WithMessage("Valid Product ID is required.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Item quantity must be greater than zero.");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative.");
        });

        RuleFor(x => x.OrderNumber)
            .MaximumLength(50).WithMessage("Order number cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.OrderNumber));
    }
}
