using FluentValidation;

namespace MyCondo.Application.Features.Security.AccessSessions.Queries.GetAccessSessionsForGuestProfile;

public sealed class GetAccessSessionsForGuestProfileQueryValidator : AbstractValidator<GetAccessSessionsForGuestProfileQuery>
{
    public GetAccessSessionsForGuestProfileQueryValidator()
    {
        RuleFor(x => x.GuestProfileId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
