using FluentValidation;

namespace MyCondo.Application.Features.Platform.Users.Queries.GetPlatformUsers;

public sealed class GetPlatformUsersQueryValidator : AbstractValidator<GetPlatformUsersQuery>
{
    public GetPlatformUsersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SearchText).MaximumLength(200);
    }
}
