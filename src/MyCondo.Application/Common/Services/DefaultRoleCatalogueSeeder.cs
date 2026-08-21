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
    ///
    /// <c>Code</c> is a stable natural key used only for idempotent reconciliation (see
    /// <see cref="SeedAsync"/>) — these remain ordinary <c>Role.CreateCustom</c> roles a tenant admin
    /// can freely rename/deactivate/re-grant; Code itself is never shown in the UI and never changes.
    /// </summary>
    private static readonly (string Name, string Code, string Description, string[] Permissions)[] DefaultRoles =
    [
        ("BuildingAdmin", "default.building-admin", "Day-to-day operational management of a single building.",
        [
            "property.view", "property.update", "resident.view", "resident.create", "resident.update",
            "ownership.view", "lease.view", "billing.rule.view", "billing.generate", "invoice.view",
            "payment.view", "payment.record", "expense.view", "expense.manage", "expense.approve",
            "expensetype.view", "expensecategory.view",
            "complaint.view", "complaint.create", "complaint.assign", "complaint.manage",
            "workorder.view", "workorder.create", "workorder.assign", "workorder.complete",
            "document.view", "document.upload", "notification.view",
        ]),
        ("Treasurer", "default.treasurer", "Tenant-wide financial oversight and correction authority.",
        [
            "billing.rule.view", "billing.rule.manage", "billing.generate", "invoice.view",
            "invoice.void", "payment.view", "payment.record", "payment.reverse", "expense.view",
            "expense.manage", "expense.approve", "expense.pay", "expensetype.view", "expensetype.manage",
            "expensecategory.view", "expensecategory.manage", "report.financial.view",
            // Billing↔Finance integration template: Fine assess/waive/reverse are correction-authority
            // actions, same category as invoice.void/payment.reverse above — Treasurer gets the full set.
            "billing.fine.view", "billing.fine.assess", "billing.fine.waive", "billing.fine.reverse",
            // Template 4: Treasurer gets full Banking/Fixed Deposit authority — placement, renewal/
            // withdrawal/void, and interest recording are all tenant-wide treasury decisions.
            "finance.bankaccount.view", "finance.bankaccount.manage", "finance.fixeddeposit.view",
            "finance.fixeddeposit.place", "finance.fixeddeposit.manage", "finance.fixeddeposit.interest.record",
            // Template 6 (governance): Treasurer is the one performing the sensitive actions the Finance
            // audit log records (period close/reopen, reversals, mapping changes, FD lifecycle) — needs
            // audit.view to review their own trail, not just Auditor's external-oversight use of it.
            "audit.view",
        ]),
        ("Secretary", "default.secretary", "Administrative/communications support — the point of contact for residents.",
        [
            "resident.view", "complaint.view", "complaint.create", "complaint.assign",
            "notification.view", "notification.manage", "document.view", "document.upload",
            "report.operational.view",
        ]),
        ("SecurityHead", "default.security-head", "Security oversight for a building.",
        [
            "complaint.view",
        ]),
        ("Owner", "default.owner", "A flat owner viewing their own ownership/billing/complaint records.",
        [
            "ownership.view", "lease.view", "invoice.view", "payment.view", "complaint.view",
            "complaint.create", "document.view",
        ]),
        ("Renter", "default.renter", "A renter viewing their lease/billing/complaint records — a strict subset of Owner.",
        [
            "lease.view", "invoice.view", "payment.view", "complaint.view", "complaint.create",
            "document.view",
        ]),
        ("Auditor", "default.auditor", "Read-only oversight across the tenant, for compliance/external audit purposes.",
        [
            "tenant.view", "user.view", "property.view", "resident.view", "ownership.view",
            "lease.view", "billing.rule.view", "invoice.view", "payment.view", "expense.view",
            "expensetype.view", "expensecategory.view", "complaint.view", "workorder.view", "document.view",
            "report.financial.view", "report.operational.view", "role.view", "permission.view",
            "audit.view",
            // Template 4: read-only Banking/Fixed Deposit visibility for compliance/external audit.
            "finance.bankaccount.view", "finance.fixeddeposit.view",
        ]),
    ];

    /// <summary>
    /// Reconciles by <c>Code</c>/<c>PermissionId</c> rather than unconditionally creating — safe to
    /// call on every tenant-bootstrap run (not just the first), so a role or permission added to the
    /// catalogue after a tenant already exists still reaches it. Never removes an existing role or
    /// grant not in the catalogue (an admin's own custom roles, or a permission grant this catalogue no
    /// longer lists, are left untouched).
    ///
    /// Falls back to matching by <see cref="Role.Name"/> for a legacy row with no <see cref="Role.Code"/>
    /// (created before <c>Role.CreateCustom</c> accepted a code — any tenant bootstrapped before this
    /// reconciliation existed) and backfills its Code, rather than attempting to create a second role
    /// with the same name, which the <c>(tenant_id, name)</c> unique constraint would reject anyway.
    /// </summary>
    public async Task SeedAsync(Guid tenantId, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        List<Permission> catalogue = await permissions.GetAllAsync(cancellationToken);
        Dictionary<string, PermissionId> permissionIdsByName = catalogue.ToDictionary(p => p.Name, p => p.Id);

        List<Role> existingRoles = await roles.GetAllForTenantAsync(tenantId, cancellationToken);
        Dictionary<string, Role> existingByCode = existingRoles
            .Where(r => r.Code is not null)
            .ToDictionary(r => r.Code!, StringComparer.Ordinal);
        Dictionary<string, Role> existingByNameWithNoCode = existingRoles
            .Where(r => r.Code is null)
            .ToDictionary(r => r.Name, StringComparer.Ordinal);

        int rolesCreated = 0;
        int rolesBackfilled = 0;
        int grantsCreated = 0;

        foreach ((string name, string code, string description, string[] permissionNames) in DefaultRoles)
        {
            Role role;
            if (existingByCode.TryGetValue(code, out Role? foundByCode))
            {
                role = foundByCode;
            }
            else if (existingByNameWithNoCode.TryGetValue(name, out Role? legacyRole))
            {
                legacyRole.BackfillCode(code);
                role = legacyRole;
                rolesBackfilled++;
            }
            else
            {
                role = Role.CreateCustom(tenantId, name, description, nowUtc, code);
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
                        $"Default role '{name}' references unknown permission '{permissionName}'.");
                }

                if (grantedIds.Add(permissionId))
                {
                    rolePermissions.Add(new RolePermission(tenantId, role.Id, permissionId, nowUtc, grantedBy: null));
                    grantsCreated++;
                }
            }
        }

        logger.LogInformation(
            "[DatabaseSeed] Default role catalogue for tenant {TenantId}: {RoleCount} roles expected, " +
            "{RolesCreated} created, {RolesBackfilled} legacy roles backfilled with a Code, " +
            "{GrantsCreated} grants created",
            tenantId, DefaultRoles.Length, rolesCreated, rolesBackfilled, grantsCreated);
    }
}
