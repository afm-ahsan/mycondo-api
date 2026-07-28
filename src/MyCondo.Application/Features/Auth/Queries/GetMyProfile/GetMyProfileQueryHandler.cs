using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Auth.Queries.GetMyProfile;

public sealed class GetMyProfileQueryHandler(
    IUserRepository users,
    IUserContextResolver userContextResolver,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetMyProfileQuery, UserProfileDto>
{
    public async ValueTask<UserProfileDto> Handle(GetMyProfileQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userIdValue)
        {
            throw new ForbiddenException("Authentication required.");
        }

        UserId userId = new(userIdValue);
        User user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userIdValue);

        return await userContextResolver.ResolveProfileAsync(user, cancellationToken);
    }
}
