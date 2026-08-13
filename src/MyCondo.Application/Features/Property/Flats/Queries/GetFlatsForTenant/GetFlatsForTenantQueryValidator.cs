using FluentValidation;

namespace MyCondo.Application.Features.Property.Flats.Queries.GetFlatsForTenant;

public sealed class GetFlatsForTenantQueryValidator : AbstractValidator<GetFlatsForTenantQuery>
{
    public GetFlatsForTenantQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}
