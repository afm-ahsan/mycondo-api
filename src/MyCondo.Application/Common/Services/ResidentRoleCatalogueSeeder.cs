using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;

namespace MyCondo.Application.Common.Services;

public sealed class ResidentRoleCatalogueSeeder(
    IRoleRepository roles,
    IPermissionRepository permissions,
    IRolePermissionRepository rolePermissions,
    ILogger<ResidentRoleCatalogueSeeder> logger
) : IResidentRoleCatalogueSeeder
{
    /// <summary>
    /// mycondo-docs ADR-021 is the source of truth for why each list looks the way it does. Neither
    /// role receives role.manage/role.view/permission.view — relationship management and role
    /// administration remain OrganizationAdmin/CondoAdmin's exclusive capability, same delegation
    /// boundary Phase 2 established for the staff condominium roles.
    /// </summary>
    private static readonly (string Name, string Code, string Description, string[] Permissions)[] ResidentRoles =
    [
        ("FlatOwner", "resident.flat-owner", "A flat owner viewing their own ownership and billing records.",
        [
            "ownership.view", "invoice.view.own",
        ]),
        ("Tenant", "resident.tenant", "A resident occupant viewing their own occupancy and billing records.",
        [
            "lease.view", "invoice.view.own",
        ]),
    ];

    /// <summary>
    /// Reconciles by <c>Code</c>/<c>PermissionId</c> rather than unconditionally creating — safe to
    /// call on every tenant-bootstrap run (not just the first), so a role or permission added to the
    /// catalogue after a tenant already exists still reaches it. Never removes an existing role or
    /// grant not in the catalogue — in particular, never grants FlatOwner/Tenant anything beyond what's
    /// listed above regardless of how many times this runs.
    /// </summary>
    public async Task SeedAsync(Guid tenantId, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        List<Permission> catalogue = await permissions.GetAllAsync(cancellationToken);
        Dictionary<string, PermissionId> permissionIdsByName = catalogue.ToDictionary(p => p.Name, p => p.Id);

        List<Role> existingRoles = await roles.GetAllForTenantAsync(tenantId, cancellationToken);
        Dictionary<string, Role> existingByCode = existingRoles
            .Where(r => r.Code is not null)
            .ToDictionary(r => r.Code!, StringComparer.Ordinal);

        int rolesCreated = 0;
        int grantsCreated = 0;

        foreach ((string name, string code, string description, string[] permissionNames) in ResidentRoles)
        {
            Role role;
            if (existingByCode.TryGetValue(code, out Role? found))
            {
                role = found;
            }
            else
            {
                role = Role.CreateSystem(
                    RoleId.New(), tenantId, name, description, nowUtc, code: code, requiresBuildingScope: true);
                roles.Add(role);
                rolesCreated++;
            }

            List<RolePermission> existingGrants = await rolePermissions.GetForRoleAsync(role.Id, cancellationToken);
            HashSet<PermissionId> grantedIds = existingGrants.Select(g => g.PermissionId).ToHashSet();

            foreach (string permissionName in permissionNames)
            {
                if (!permissionIdsByName.TryGetValue(permissionName, out PermissionId permissionId))
                {
                    throw new InvalidOperationException(
                        $"Resident role '{name}' references unknown permission '{permissionName}'.");
                }

                if (grantedIds.Add(permissionId))
                {
                    rolePermissions.Add(new RolePermission(tenantId, role.Id, permissionId, nowUtc, grantedBy: null));
                    grantsCreated++;
                }
            }
        }

        logger.LogInformation(
            "[DatabaseSeed] Resident role catalogue for tenant {TenantId}: {RoleCount} roles expected, " +
            "{RolesCreated} created, {GrantsCreated} grants created",
            tenantId, ResidentRoles.Length, rolesCreated, grantsCreated);
    }
}
