using FluentValidation;

namespace MyCondo.Application.Features.Billing.Queries.GetServiceChargeRules;

public sealed class GetServiceChargeRulesQueryValidator : AbstractValidator<GetServiceChargeRulesQuery>
{
    public GetServiceChargeRulesQueryValidator()
    {
        RuleFor(x => x.BuildingId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
