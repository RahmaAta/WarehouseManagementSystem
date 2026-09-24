using FluentValidation;
using WarehouseManagement.Application.Features.Reports.Queries.GetSalesOrderSummaryReport;

namespace WarehouseManagement.Application.Features.Reports.Queries.GetSalesOrderSummaryReport;

public class GetSalesOrderSummaryReportQueryValidator
    : AbstractValidator<GetSalesOrderSummaryReportQuery>
{
    public GetSalesOrderSummaryReportQueryValidator()
    {
        RuleFor(x => x.From)
            .LessThan(x => x.To)
            .When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("'From' date must be earlier than 'To' date.");

        RuleFor(x => x.To)
            .LessThanOrEqualTo(_ => DateTimeOffset.UtcNow)
            .When(x => x.To.HasValue)
            .WithMessage("'To' date cannot be in the future.");

        RuleFor(x => x.TopCustomersCount)
            .InclusiveBetween(1, 50)
            .WithMessage("TopCustomersCount must be between 1 and 50.");
    }
}
