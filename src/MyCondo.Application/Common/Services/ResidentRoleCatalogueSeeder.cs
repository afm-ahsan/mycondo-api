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

    public async Task SeedAsync(Guid tenantId, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        List<Permission> catalogue = await permissions.GetAllAsync(cancellationToken);
        Dictionary<string, PermissionId> permissionIdsByName = catalogue.ToDictionary(p => p.Name, p => p.Id);

        foreach ((string name, string code, string description, string[] permissionNames) in ResidentRoles)
        {
            Role role = Role.CreateSystem(
                RoleId.New(), tenantId, name, description, nowUtc, code: code, requiresBuildingScope: true);
            roles.Add(role);

            foreach (string permissionName in permissionNames)
            {
                if (!permissionIdsByName.TryGetValue(permissionName, out PermissionId permissionId))
                {
                    throw new InvalidOperationException(
                        $"Resident role '{name}' references unknown permission '{permissionName}'.");
                }

                rolePermissions.Add(new RolePermission(tenantId, role.Id, permissionId, nowUtc, grantedBy: null));
            }
        }

        logger.LogInformation(
            "Resident role catalogue seeded for tenant {TenantId}: {RoleCount} roles",
            tenantId, ResidentRoles.Length);
    }
}
