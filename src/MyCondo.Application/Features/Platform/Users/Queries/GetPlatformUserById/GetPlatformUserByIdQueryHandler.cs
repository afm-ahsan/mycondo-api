using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Users.DTOs;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Application.Features.Platform.Users.Queries.GetPlatformUserById;

public sealed class GetPlatformUserByIdQueryHandler(
    IPlatformUserRepository platformUsers,
    ICurrentPlatformUserProvider currentUser
) : IRequestHandler<GetPlatformUserByIdQuery, PlatformUserDetailDto>
{
    public async ValueTask<PlatformUserDetailDto> Handle(
        GetPlatformUserByIdQuery query, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PlatformUserId platformUserId = new(query.PlatformUserId);
        PlatformUser user = await platformUsers.GetByIdAsync(platformUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(PlatformUser), query.PlatformUserId);

        return new PlatformUserDetailDto(
            user.Id.Value,
            user.Email,
            user.DisplayName,
            user.Status == PlatformUserStatus.Active,
            user.LastLoginAtUtc,
            user.CreatedAtUtc,
            user.UpdatedAtUtc);
    }
}
