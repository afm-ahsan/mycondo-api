using FluentValidation;

namespace MyCondo.Application.Features.Security.Guests.Queries.GetGuestProfilesForTenant;

public sealed class GetGuestProfilesForTenantQueryValidator : AbstractValidator<GetGuestProfilesForTenantQuery>
{
    public GetGuestProfilesForTenantQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}
