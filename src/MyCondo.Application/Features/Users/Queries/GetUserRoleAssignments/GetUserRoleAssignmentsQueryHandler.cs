using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Users.Queries.GetUserRoleAssignments;

public sealed class GetUserRoleAssignmentsQueryHandler(
    IUserRepository users,
    IRoleRepository roles,
    IRoleAssignmentRepository roleAssignments,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetUserRoleAssignmentsQuery, List<UserRoleAssignmentDto>>
{
    public async ValueTask<List<UserRoleAssignmentDto>> Handle(
        GetUserRoleAssignmentsQuery query, CancellationToken cancellationToken)
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

        List<RoleAssignment> assignments = await roleAssignments.GetForUserAsync(tenantId, userId, cancellationToken);
        List<Role> tenantRoles = await roles.GetAllForTenantAsync(tenantId, cancellationToken);
        Dictionary<RoleId, Role> rolesById = tenantRoles.ToDictionary(r => r.Id);

        return assignments
            .Where(a => rolesById.ContainsKey(a.RoleId))
            .Select(a =>
            {
                Role role = rolesById[a.RoleId];
                return new UserRoleAssignmentDto(
                    role.Id.Value, role.Name, role.Code, role.RequiresBuildingScope, a.BuildingId);
            })
            .ToList();
    }
}
