using FluentValidation;

namespace MyCondo.Application.Features.Billing.Queries.GetFlatsMissingArea;

public sealed class GetFlatsMissingAreaQueryValidator : AbstractValidator<GetFlatsMissingAreaQuery>
{
    public GetFlatsMissingAreaQueryValidator()
    {
        RuleFor(x => x.BuildingId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
