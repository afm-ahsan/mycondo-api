using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Users.Queries.GetUsersForTenant;

public sealed class GetUsersForTenantQueryHandler(
    IUserRepository users,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetUsersForTenantQuery, List<UserSummaryDto>>
{
    public async ValueTask<List<UserSummaryDto>> Handle(GetUsersForTenantQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        List<User> tenantUsers = await users.GetAllForTenantAsync(tenantId, cancellationToken);

        return tenantUsers
            .Select(u => new UserSummaryDto(u.Id.Value, u.Email, u.FullName, u.Status == UserStatus.Active))
            .ToList();
    }
}
