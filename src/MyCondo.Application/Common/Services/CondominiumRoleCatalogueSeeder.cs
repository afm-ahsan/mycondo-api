using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;

namespace MyCondo.Application.Common.Services;

public sealed class CondominiumRoleCatalogueSeeder(
    IRoleRepository roles,
    IPermissionRepository permissions,
    IRolePermissionRepository rolePermissions,
    ILogger<CondominiumRoleCatalogueSeeder> logger
) : ICondominiumRoleCatalogueSeeder
{
    /// <summary>
    /// mycondo-docs ADR-020 / the Phase-2 role catalogue addendum to ROLE_CATALOGUE_PROPOSAL.md is the
    /// source of truth for *why* each list looks the way it does. Each role mirrors the closest
    /// existing <c>DefaultRoleCatalogueSeeder</c> custom role's reviewed permission set where one
    /// exists (CondoAdmin~BuildingAdmin, Manager~Secretary, Accountant~Treasurer's operational subset),
    /// plus SecurityOfficer/FacilityManager built fresh from modules that shipped after that proposal
    /// was written. Every permission listed here must already exist in the catalogue — Phase 2 adds no
    /// new permission codes.
    /// </summary>
    private static readonly (string Name, string Code, string Description, string[] Permissions)[] CondominiumRoles =
    [
        ("CondoAdmin", "condominium.admin", "Day-to-day operational management of a single condominium/building.",
        [
            "property.view", "property.update", "resident.view", "resident.create", "resident.update",
            "ownership.view", "ownership.manage", "lease.view", "billing.rule.view", "billing.generate", "invoice.view",
            "payment.view", "payment.record", "expense.view", "expense.manage", "expensetype.view",
            "complaint.view", "complaint.create", "complaint.assign", "complaint.manage", "workorder.view",
            "workorder.create", "workorder.assign", "workorder.complete", "document.view",
            "document.upload", "notification.view", "visitor.override", "vehicle.override",
        ]),
        ("Manager", "condominium.manager", "Administrative and communications support for a single condominium/building.",
        [
            "resident.view", "complaint.view", "complaint.create", "complaint.assign",
            "notification.view", "notification.manage", "document.view", "document.upload",
            "report.operational.view",
        ]),
        ("Accountant", "condominium.accountant", "Financial administration for a single condominium/building.",
        [
            "billing.rule.view", "billing.generate", "invoice.view", "payment.view", "payment.record",
            "expense.view", "expense.manage", "expensetype.view", "report.financial.view",
            // Billing↔Finance integration template: Accountant gets Treasurer's operational subset
            // (view + assess) — not waive/reverse, mirroring its existing exclusion of invoice.void/
            // payment.reverse (correction authority stays with the tenant-wide Treasurer role).
            "billing.fine.view", "billing.fine.assess",
        ]),
        ("SecurityOfficer", "condominium.security", "Security, visitor, vehicle, and parcel oversight for a single condominium/building.",
        [
            "visitor.view", "visitor.create", "visitor.checkin", "visitor.checkout", "visitor.block.manage",
            "vehicle.view", "vehicle.create", "vehicle.checkin", "vehicle.checkout", "vehicle.block.manage",
            "parcel.view", "parcel.receive", "parcel.handover", "parcel.notify", "parcel.escalate",
            "parcel.return", "parcel.update", "report.security.view", "complaint.view",
        ]),
        ("FacilityManager", "condominium.facility-manager", "Amenities, pool, generator, and gas cylinder operations for a single condominium/building.",
        [
            "facility.view", "facility.manage", "facility.booking.view", "facility.booking.approve",
            "facility.booking.cancel", "facility.booking.inspect", "pool.view", "pool.manage",
            "pool.checkin", "pool.checkout", "pool.incident.manage", "generator.view", "generator.manage",
            "generator.operation.manage", "generator.fuel.manage", "generator.maintenance.manage",
            "gascylinder.view", "gascylinder.stock.manage", "report.facility",
        ]),
    ];

    /// <summary>
    /// Reconciles by <c>Code</c>/<c>PermissionId</c> rather than unconditionally creating — safe to
    /// call on every tenant-bootstrap run (not just the first), so a role or permission added to the
    /// catalogue after a tenant already exists still reaches it. Never removes an existing role or
    /// grant not in the catalogue.
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

        foreach ((string name, string code, string description, string[] permissionNames) in CondominiumRoles)
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
                        $"Condominium role '{name}' references unknown permission '{permissionName}'.");
                }

                if (grantedIds.Add(permissionId))
                {
                    rolePermissions.Add(new RolePermission(tenantId, role.Id, permissionId, nowUtc, grantedBy: null));
                    grantsCreated++;
                }
            }
        }

        logger.LogInformation(
            "[DatabaseSeed] Condominium role catalogue for tenant {TenantId}: {RoleCount} roles expected, " +
            "{RolesCreated} created, {GrantsCreated} grants created",
            tenantId, CondominiumRoles.Length, rolesCreated, grantsCreated);
    }
}
