using FluentValidation;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetFinancialOverview;

public sealed class GetFinancialOverviewQueryValidator : AbstractValidator<GetFinancialOverviewQuery>
{
    public GetFinancialOverviewQueryValidator()
    {
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate!.Value)
            .When(x => x.FromDate is not null && x.ToDate is not null)
            .WithMessage("ToDate must not be before FromDate.");
    }
}
