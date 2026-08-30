using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Users.DTOs;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Application.Features.Platform.Users.Queries.GetPlatformUsers;

public sealed class GetPlatformUsersQueryHandler(
    IPlatformUserRepository platformUsers,
    IPlatformUserRoleAssignmentRepository platformUserRoleAssignments,
    IPlatformRoleRepository platformRoles,
    ICurrentPlatformUserProvider currentUser
) : IRequestHandler<GetPlatformUsersQuery, PagedResult<PlatformUserSummaryDto>>
{
    public async ValueTask<PagedResult<PlatformUserSummaryDto>> Handle(
        GetPlatformUsersQuery query, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PagedResult<PlatformUser> result = await platformUsers.SearchAsync(
            query.SearchText, query.Status, query.Page, query.PageSize, cancellationToken);

        List<PlatformRole> allRoles = await platformRoles.GetAllAsync(cancellationToken);
        Dictionary<PlatformRoleId, string> roleNamesById = allRoles.ToDictionary(r => r.Id, r => r.Name);

        // Batched per-user lookup — there is no "get for users" batch method on this repository yet,
        // so this mirrors the tenant pattern's shape at Platform scale (few operator accounts expected).
        Dictionary<PlatformUserId, List<string>> roleNamesByUserId = [];
        foreach (PlatformUser user in result.Items)
        {
            List<PlatformUserRoleAssignment> assignments = await platformUserRoleAssignments.GetForUserAsync(
                user.Id, cancellationToken);
            roleNamesByUserId[user.Id] = assignments
                .Where(a => roleNamesById.ContainsKey(a.PlatformRoleId))
                .Select(a => roleNamesById[a.PlatformRoleId])
                .Distinct()
                .ToList();
        }

        List<PlatformUserSummaryDto> items = result.Items
            .Select(u => new PlatformUserSummaryDto(
                u.Id.Value, u.Email, u.DisplayName, u.Status == PlatformUserStatus.Active,
                u.LastLoginAtUtc, u.CreatedAtUtc,
                roleNamesByUserId.TryGetValue(u.Id, out List<string>? names) ? names : []))
            .ToList();

        return new PagedResult<PlatformUserSummaryDto>(items, result.Page, result.PageSize, result.Total);
    }
}
