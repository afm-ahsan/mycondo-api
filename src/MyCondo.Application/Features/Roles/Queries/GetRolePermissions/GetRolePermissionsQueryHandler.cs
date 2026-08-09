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
    // Same constant/comment as GetPermissionCatalogueQueryHandler and
    // GrantPermissionToRoleCommandHandler. GrantPermissionToRoleCommandHandler already refuses to ever
    // attach a platform.* permission to a tenant role, so grantedIds below should never legitimately
    // contain one — this filter is defense-in-depth, not the primary control, in case some other write
    // path (a future bug, a raw DB insert) ever bypasses that check.
    private const string PlatformPermissionModule = "platform";

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
            .Where(p => grantedIds.Contains(p.Id) && !string.Equals(p.Module, PlatformPermissionModule, StringComparison.Ordinal))
            .Select(p => new PermissionDto(p.Id.Value, p.Name, p.Description, p.Module, p.IsBuildingScopable))
            .ToList();
    }
}
