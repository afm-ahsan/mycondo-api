using FluentValidation;

namespace MyCondo.Application.Features.Security.ServiceProviders.Queries.GetServiceProviderProfilesForTenant;

public sealed class GetServiceProviderProfilesForTenantQueryValidator : AbstractValidator<GetServiceProviderProfilesForTenantQuery>
{
    public GetServiceProviderProfilesForTenantQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}
