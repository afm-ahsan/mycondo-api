using FluentValidation;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseByCategoryReport;

public sealed class GetExpenseByCategoryReportQueryValidator : AbstractValidator<GetExpenseByCategoryReportQuery>
{
    public GetExpenseByCategoryReportQueryValidator()
    {
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate).WithMessage("ToDate must not be before FromDate.");
    }
}
