using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Common.Services;

public sealed class SuperAdminBootstrapper(
    IRoleRepository roles,
    IPermissionRepository permissions,
    IRolePermissionRepository rolePermissions,
    IRoleAssignmentRepository roleAssignments,
    ILogger<SuperAdminBootstrapper> logger
) : ISuperAdminBootstrapper
{
    private const string SuperAdminRoleName = "SuperAdmin";

    public async Task BootstrapAsync(
        Guid tenantId, User user, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        Role superAdmin = Role.CreateSystem(
            RoleId.New(),
            tenantId,
            SuperAdminRoleName,
            "Full access to all permissions (auto-provisioned for the tenant's first user).",
            nowUtc);

        roles.Add(superAdmin);

        List<Permission> catalogue = await permissions.GetAllAsync(cancellationToken);
        foreach (Permission permission in catalogue)
        {
            rolePermissions.Add(new RolePermission(tenantId, superAdmin.Id, permission.Id, nowUtc, grantedBy: null));
        }

        roleAssignments.Add(RoleAssignment.Grant(tenantId, user.Id, superAdmin.Id, buildingId: null, nowUtc));

        logger.LogInformation(
            "SuperAdmin role {RoleId} bootstrapped for tenant {TenantId} with {PermissionCount} permissions",
            superAdmin.Id, tenantId, catalogue.Count);
    }
}
