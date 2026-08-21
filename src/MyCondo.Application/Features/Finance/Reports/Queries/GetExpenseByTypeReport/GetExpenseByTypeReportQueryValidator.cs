using FluentValidation;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseByTypeReport;

public sealed class GetExpenseByTypeReportQueryValidator : AbstractValidator<GetExpenseByTypeReportQuery>
{
    public GetExpenseByTypeReportQueryValidator()
    {
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate).WithMessage("ToDate must not be before FromDate.");
    }
}
