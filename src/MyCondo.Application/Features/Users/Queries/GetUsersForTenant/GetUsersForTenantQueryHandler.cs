using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Users.Queries.GetUsersForTenant;

public sealed class GetUsersForTenantQueryHandler(
    IUserRepository users,
    IRoleAssignmentRepository roleAssignments,
    IRoleRepository roles,
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

        List<UserId> pageUserIds = result.Items.Select(u => u.Id).ToList();
        List<RoleAssignment> assignments = await roleAssignments.GetForUsersAsync(tenantId, pageUserIds, cancellationToken);
        List<Role> tenantRoles = await roles.GetAllForTenantAsync(tenantId, cancellationToken);
        Dictionary<RoleId, string> roleNamesById = tenantRoles.ToDictionary(r => r.Id, r => r.Name);

        Dictionary<UserId, List<string>> roleNamesByUserId = assignments
            .Where(a => roleNamesById.ContainsKey(a.RoleId))
            .GroupBy(a => a.UserId)
            .ToDictionary(g => g.Key, g => g.Select(a => roleNamesById[a.RoleId]).Distinct().ToList());

        List<UserSummaryDto> items = result.Items
            .Select(u => new UserSummaryDto(
                u.Id.Value, u.Email, u.FullName, u.PhoneNumber, u.Status == UserStatus.Active,
                u.LastLoginAtUtc, u.CreatedAtUtc,
                roleNamesByUserId.TryGetValue(u.Id, out List<string>? names) ? names : []))
            .ToList();

        return new PagedResult<UserSummaryDto>(items, result.Page, result.PageSize, result.Total);
    }
}
