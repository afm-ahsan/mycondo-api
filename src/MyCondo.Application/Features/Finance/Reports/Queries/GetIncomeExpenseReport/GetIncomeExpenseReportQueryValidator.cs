using FluentValidation;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetIncomeExpenseReport;

public sealed class GetIncomeExpenseReportQueryValidator : AbstractValidator<GetIncomeExpenseReportQuery>
{
    public GetIncomeExpenseReportQueryValidator()
    {
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("ToDate must not be before FromDate.");
    }
}
