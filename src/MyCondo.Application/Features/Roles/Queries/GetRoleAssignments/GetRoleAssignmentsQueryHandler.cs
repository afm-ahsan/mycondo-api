using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Roles.Queries.GetRoleAssignments;

public sealed class GetRoleAssignmentsQueryHandler(
    IRoleRepository roles,
    IUserRepository users,
    IRoleAssignmentRepository roleAssignments,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetRoleAssignmentsQuery, List<RoleAssignmentDto>>
{
    public async ValueTask<List<RoleAssignmentDto>> Handle(GetRoleAssignmentsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        RoleId roleId = new(query.RoleId);
        Role role = await roles.GetByIdAsync(roleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), query.RoleId);

        if (role.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Role), query.RoleId);
        }

        List<RoleAssignment> assignments = await roleAssignments.GetForRoleAsync(tenantId, roleId, cancellationToken);
        List<User> tenantUsers = await users.GetAllForTenantAsync(tenantId, cancellationToken);
        Dictionary<UserId, User> usersById = tenantUsers.ToDictionary(u => u.Id);

        return assignments
            .Where(a => usersById.ContainsKey(a.UserId))
            .Select(a =>
            {
                User user = usersById[a.UserId];
                return new RoleAssignmentDto(user.Id.Value, user.Email, user.FullName, a.BuildingId);
            })
            .ToList();
    }
}
