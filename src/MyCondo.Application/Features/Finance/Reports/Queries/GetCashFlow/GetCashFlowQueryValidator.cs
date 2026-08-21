using FluentValidation;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetCashFlow;

public sealed class GetCashFlowQueryValidator : AbstractValidator<GetCashFlowQuery>
{
    public GetCashFlowQueryValidator()
    {
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate).WithMessage("ToDate must not be before FromDate.");
    }
}
