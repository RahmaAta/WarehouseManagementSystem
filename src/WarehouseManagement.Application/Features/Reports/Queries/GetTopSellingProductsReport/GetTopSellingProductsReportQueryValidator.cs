using FluentValidation;
using WarehouseManagement.Application.Features.Reports.Queries.GetTopSellingProductsReport;

namespace WarehouseManagement.Application.Features.Reports.Queries.GetTopSellingProductsReport;

public class GetTopSellingProductsReportQueryValidator
    : AbstractValidator<GetTopSellingProductsReportQuery>
{
    public GetTopSellingProductsReportQueryValidator()
    {
        RuleFor(x => x.From)
            .LessThan(x => x.To)
            .When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("'From' date must be earlier than 'To' date.");

        RuleFor(x => x.To)
            .LessThanOrEqualTo(_ => DateTimeOffset.UtcNow)
            .When(x => x.To.HasValue)
            .WithMessage("'To' date cannot be in the future.");

        RuleFor(x => x.TopN)
            .InclusiveBetween(1, 100)
            .WithMessage("TopN must be between 1 and 100.");
    }
}
