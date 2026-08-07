using FluentValidation;

namespace MyCondo.Application.Features.Operations.Queries.GetMonthlyReconciliations;

public sealed class GetMonthlyReconciliationsQueryValidator : AbstractValidator<GetMonthlyReconciliationsQuery>
{
    public GetMonthlyReconciliationsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
