namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Seeds a tenant's default custom roles — <c>mycondo-docs/07-delivery/ROLE_CATALOGUE_PROPOSAL.md</c>,
/// approved 2026-07-28 — resolving <c>MASTER_BACKLOG.md</c> ID-7. Unlike <see cref="IOrganizationAdminBootstrapper"/>'s
/// OrganizationAdmin role, these are ordinary <c>Role.CreateCustom</c> roles: a tenant admin can rename,
/// deactivate, or re-grant them freely, and nothing is auto-assigned to any user — they exist so the
/// OrganizationAdmin has something sensible to hand out. Vendor and Guard from the sketch are deliberately
/// NOT seeded: their modules haven't shipped any catalogue permissions yet (an empty role would be
/// useless), per the proposal's own "Open items" note.
/// </summary>
public interface IDefaultRoleCatalogueSeeder
{
    /// <summary>
    /// Runs alongside <see cref="IOrganizationAdminBootstrapper.BootstrapAsync"/> at tenant-bootstrap time
    /// (first user registration, <c>MyCondo.DbMigrator</c>'s production bootstrap command) — same
    /// caller owns <c>IUnitOfWork.SaveChangesAsync</c> afterward. Idempotent by reconciling against each
    /// role's <c>Code</c> and each grant's <c>PermissionId</c> — safe to call again for a tenant that
    /// already has some or all of these roles (e.g. <c>ArpDevelopmentBootstrapSeeder</c> calls this on
    /// every startup, not just when the tenant is first created), so a role/permission added to the
    /// catalogue later still reaches an already-bootstrapped tenant.
    /// </summary>
    Task SeedAsync(Guid tenantId, DateTimeOffset nowUtc, CancellationToken cancellationToken);
}
