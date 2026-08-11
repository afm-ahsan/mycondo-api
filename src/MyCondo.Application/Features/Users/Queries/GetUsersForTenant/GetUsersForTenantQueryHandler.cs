using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Users.Queries.GetUsersForTenant;

public sealed class GetUsersForTenantQueryHandler(
    IUserRepository users,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetUsersForTenantQuery, PagedResult<UserSummaryDto>>
{
    public async ValueTask<PagedResult<UserSummaryDto>> Handle(
        GetUsersForTenantQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PagedResult<User> result = await users.SearchAsync(
            tenantId, query.SearchText, query.RoleId, query.IsActive, query.Page, query.PageSize, cancellationToken);

        List<UserSummaryDto> items = result.Items
            .Select(u => new UserSummaryDto(
                u.Id.Value, u.Email, u.FullName, u.PhoneNumber, u.Status == UserStatus.Active,
                u.LastLoginAtUtc, u.CreatedAtUtc))
            .ToList();

        return new PagedResult<UserSummaryDto>(items, result.Page, result.PageSize, result.Total);
    }
}
