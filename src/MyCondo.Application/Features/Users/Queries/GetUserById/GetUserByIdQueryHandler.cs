using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler(
    IUserRepository users,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetUserByIdQuery, UserDetailDto>
{
    public async ValueTask<UserDetailDto> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        UserId userId = new(query.UserId);
        User user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), query.UserId);

        if (user.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(User), query.UserId);
        }

        return new UserDetailDto(
            user.Id.Value,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.Status == UserStatus.Active,
            user.EmailConfirmed,
            user.LastLoginAtUtc,
            user.CreatedAtUtc,
            user.UpdatedAtUtc);
    }
}
