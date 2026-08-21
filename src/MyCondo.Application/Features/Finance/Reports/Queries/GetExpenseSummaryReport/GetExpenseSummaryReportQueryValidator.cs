using FluentValidation;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseSummaryReport;

public sealed class GetExpenseSummaryReportQueryValidator : AbstractValidator<GetExpenseSummaryReportQuery>
{
    public GetExpenseSummaryReportQueryValidator()
    {
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate).WithMessage("ToDate must not be before FromDate.");
    }
}
