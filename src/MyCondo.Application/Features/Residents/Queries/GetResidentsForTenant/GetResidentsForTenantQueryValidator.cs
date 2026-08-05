using FluentValidation;

namespace MyCondo.Application.Features.Residents.Queries.GetResidentsForTenant;

public sealed class GetResidentsForTenantQueryValidator : AbstractValidator<GetResidentsForTenantQuery>
{
    public GetResidentsForTenantQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}
