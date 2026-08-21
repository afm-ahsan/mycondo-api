using FluentValidation;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseTrendReport;

public sealed class GetExpenseTrendReportQueryValidator : AbstractValidator<GetExpenseTrendReportQuery>
{
    public GetExpenseTrendReportQueryValidator()
    {
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate).WithMessage("ToDate must not be before FromDate.");
    }
}
