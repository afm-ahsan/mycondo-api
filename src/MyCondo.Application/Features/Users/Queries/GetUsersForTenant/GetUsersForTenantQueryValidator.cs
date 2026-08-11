using FluentValidation;

namespace MyCondo.Application.Features.Users.Queries.GetUsersForTenant;

public sealed class GetUsersForTenantQueryValidator : AbstractValidator<GetUsersForTenantQuery>
{
    public GetUsersForTenantQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SearchText).MaximumLength(200);
    }
}
