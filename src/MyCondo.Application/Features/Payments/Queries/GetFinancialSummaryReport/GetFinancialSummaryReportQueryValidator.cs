using FluentValidation;

namespace MyCondo.Application.Features.Payments.Queries.GetFinancialSummaryReport;

public sealed class GetFinancialSummaryReportQueryValidator : AbstractValidator<GetFinancialSummaryReportQuery>
{
    public GetFinancialSummaryReportQueryValidator()
    {
        RuleFor(x => x.FromDate).NotEmpty();
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("ToDate must not precede FromDate.");
    }
}
