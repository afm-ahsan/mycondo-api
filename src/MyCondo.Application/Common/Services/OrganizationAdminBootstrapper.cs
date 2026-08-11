using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Common.Services;

public sealed class OrganizationAdminBootstrapper(
    IRoleRepository roles,
    IPermissionRepository permissions,
    IRolePermissionRepository rolePermissions,
    IRoleAssignmentRepository roleAssignments,
    ILogger<OrganizationAdminBootstrapper> logger
) : IOrganizationAdminBootstrapper
{
    private const string OrganizationAdminRoleName = "OrganizationAdmin";
    private const string OrganizationAdminRoleCode = "organization.admin";

    // Lowercase, matching every other permission Module value in the catalog — see
    // Seed_Permission_Catalogue.cs. Excluded here so a tenant's OrganizationAdmin never receives the
    // platform.* permissions Phase 1 added to this same shared table (mycondo-docs ADR-020) — this is
    // the fix for the defect the legacy SuperAdmin bootstrap had (see this class's former name,
    // SuperAdminBootstrapper, which granted the unfiltered catalogue).
    private const string PlatformPermissionModule = "platform";

    // tenant.view/tenant.manage govern SaaS-tenant (organization) lifecycle — provisioning,
    // activation, suspension — which belongs exclusively to Platform Administration
    // (platform.organization.*, checked via RequirePlatformPermission). Excluded from the
    // OrganizationAdmin's blanket grant for the same reason as "platform" above: this was the root
    // cause of a latent cross-boundary escalation (every tenant's OrganizationAdmin held tenant.manage
    // by default, and a since-removed tenant-side endpoint accepted it) — see mycondo-docs Create
    // Tenant audit decision. Defense in depth even with that endpoint gone.
    private const string TenantLifecyclePermissionModule = "tenant";

    public async Task BootstrapAsync(
        Guid tenantId, User user, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        Role organizationAdmin = Role.CreateSystem(
            RoleId.New(),
            tenantId,
            OrganizationAdminRoleName,
            "Tenant-wide administrative authority over the organization and all its condominiums (auto-provisioned for the tenant's first user).",
            nowUtc,
            code: OrganizationAdminRoleCode,
            requiresBuildingScope: false);

        roles.Add(organizationAdmin);

        List<Permission> catalogue = await permissions.GetAllAsync(cancellationToken);
        List<Permission> tenantPermissions = catalogue
            .Where(p => !string.Equals(p.Module, PlatformPermissionModule, StringComparison.Ordinal)
                && !string.Equals(p.Module, TenantLifecyclePermissionModule, StringComparison.Ordinal))
            .ToList();

        foreach (Permission permission in tenantPermissions)
        {
            rolePermissions.Add(new RolePermission(tenantId, organizationAdmin.Id, permission.Id, nowUtc, grantedBy: null));
        }

        roleAssignments.Add(RoleAssignment.Grant(tenantId, user.Id, organizationAdmin.Id, buildingId: null, nowUtc));

        logger.LogInformation(
            "OrganizationAdmin role {RoleId} bootstrapped for tenant {TenantId} with {PermissionCount} permissions",
            organizationAdmin.Id, tenantId, tenantPermissions.Count);
    }
}
