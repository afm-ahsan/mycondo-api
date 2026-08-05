using FluentValidation;

namespace MyCondo.Application.Features.Property.Buildings.Queries.GetBuildingsForTenant;

public sealed class GetBuildingsForTenantQueryValidator : AbstractValidator<GetBuildingsForTenantQuery>
{
    public GetBuildingsForTenantQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}
