using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;

namespace MyCondo.Application.Common.Services;

public sealed class DefaultRoleCatalogueSeeder(
    IRoleRepository roles,
    IPermissionRepository permissions,
    IRolePermissionRepository rolePermissions,
    ILogger<DefaultRoleCatalogueSeeder> logger
) : IDefaultRoleCatalogueSeeder
{
    /// <summary>
    /// mycondo-docs/07-delivery/ROLE_CATALOGUE_PROPOSAL.md, approved 2026-07-28. Permission lists are
    /// copied verbatim from the proposal's per-role "Assigned permissions" — that document is the
    /// source of truth for *why* each list looks the way it does (exclusions, rationale). The sketch's
    /// "Tenant" role is seeded here as "Renter" (per the proposal's own naming-collision note) to avoid
    /// colliding with this platform's own tenant (customer organization) concept. Vendor and Guard are
    /// excluded entirely — zero implementable permissions until Vendor Management / Security-Visitor
    /// Management ship.
    /// </summary>
    private static readonly (string Name, string Description, string[] Permissions)[] DefaultRoles =
    [
        ("BuildingAdmin", "Day-to-day operational management of a single building.",
        [
            "property.view", "property.update", "resident.view", "resident.create", "resident.update",
            "ownership.view", "lease.view", "billing.rule.view", "billing.generate", "invoice.view",
            "payment.view", "payment.record", "expense.view", "expense.manage", "complaint.view",
            "complaint.create", "complaint.assign", "complaint.manage", "workorder.view",
            "workorder.create", "workorder.assign", "workorder.complete", "document.view",
            "document.upload", "notification.view",
        ]),
        ("Treasurer", "Tenant-wide financial oversight and correction authority.",
        [
            "billing.rule.view", "billing.rule.manage", "billing.generate", "invoice.view",
            "invoice.void", "payment.view", "payment.record", "payment.reverse", "expense.view",
            "expense.manage", "report.financial.view",
        ]),
        ("Secretary", "Administrative/communications support — the point of contact for residents.",
        [
            "resident.view", "complaint.view", "complaint.create", "complaint.assign",
            "notification.view", "notification.manage", "document.view", "document.upload",
            "report.operational.view",
        ]),
        ("SecurityHead", "Security oversight for a building.",
        [
            "complaint.view",
        ]),
        ("Owner", "A flat owner viewing their own ownership/billing/complaint records.",
        [
            "ownership.view", "lease.view", "invoice.view", "payment.view", "complaint.view",
            "complaint.create", "document.view",
        ]),
        ("Renter", "A renter viewing their lease/billing/complaint records — a strict subset of Owner.",
        [
            "lease.view", "invoice.view", "payment.view", "complaint.view", "complaint.create",
            "document.view",
        ]),
        ("Auditor", "Read-only oversight across the tenant, for compliance/external audit purposes.",
        [
            "tenant.view", "user.view", "property.view", "resident.view", "ownership.view",
            "lease.view", "billing.rule.view", "invoice.view", "payment.view", "expense.view",
            "complaint.view", "workorder.view", "document.view", "report.financial.view",
            "report.operational.view", "role.view", "permission.view", "audit.view",
        ]),
    ];

    public async Task SeedAsync(Guid tenantId, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        List<Permission> catalogue = await permissions.GetAllAsync(cancellationToken);
        Dictionary<string, PermissionId> permissionIdsByName = catalogue.ToDictionary(p => p.Name, p => p.Id);

        foreach ((string name, string description, string[] permissionNames) in DefaultRoles)
        {
            Role role = Role.CreateCustom(tenantId, name, description, nowUtc);
            roles.Add(role);

            foreach (string permissionName in permissionNames)
            {
                if (!permissionIdsByName.TryGetValue(permissionName, out PermissionId permissionId))
                {
                    throw new InvalidOperationException(
                        $"Default role '{name}' references unknown permission '{permissionName}'.");
                }

                rolePermissions.Add(new RolePermission(tenantId, role.Id, permissionId, nowUtc, grantedBy: null));
            }
        }

        logger.LogInformation(
            "Default role catalogue seeded for tenant {TenantId}: {RoleCount} roles",
            tenantId, DefaultRoles.Length);
    }
}
