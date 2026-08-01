using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Roles.Queries.GetPermissionCatalogue;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;

namespace MyCondo.Application.Features.Roles.Queries.GetRolePermissions;

public sealed class GetRolePermissionsQueryHandler(
    IRoleRepository roles,
    IPermissionRepository permissions,
    IRolePermissionRepository rolePermissions,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetRolePermissionsQuery, List<PermissionDto>>
{
    public async ValueTask<List<PermissionDto>> Handle(GetRolePermissionsQuery query, CancellationToken cancellationToken)
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

        List<RolePermission> grants = await rolePermissions.GetForRoleAsync(roleId, cancellationToken);
        HashSet<PermissionId> grantedIds = grants.Select(g => g.PermissionId).ToHashSet();

        List<Permission> catalogue = await permissions.GetAllAsync(cancellationToken);

        return catalogue
            .Where(p => grantedIds.Contains(p.Id))
            .Select(p => new PermissionDto(p.Id.Value, p.Name, p.Description, p.Module, p.IsBuildingScopable))
            .ToList();
    }
}
