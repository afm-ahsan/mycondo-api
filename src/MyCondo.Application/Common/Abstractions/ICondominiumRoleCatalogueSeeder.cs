namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Seeds a tenant's five condominium-scoped <em>system</em> roles — CondoAdmin, Manager, Accountant,
/// SecurityOfficer, FacilityManager (mycondo-docs ADR-020, Phase 2). Unlike
/// <see cref="IDefaultRoleCatalogueSeeder"/>'s custom roles, these are <c>Role.CreateSystem</c> roles
/// with <c>RequiresBuildingScope = true</c> — every assignment of one of these roles must carry a
/// BuildingId (enforced by <c>AssignRoleToUserCommandHandler</c>), and none of them can be renamed or
/// deactivated. Nothing is auto-assigned to any user; they exist so the tenant's OrganizationAdmin has
/// a ready-made, correctly-scoped catalogue to hand out.
/// </summary>
public interface ICondominiumRoleCatalogueSeeder
{
    /// <summary>
    /// Runs alongside <see cref="IOrganizationAdminBootstrapper.BootstrapAsync"/> and
    /// <see cref="IDefaultRoleCatalogueSeeder.SeedAsync"/> at tenant-bootstrap time (first user
    /// registration, <c>MyCondo.DbMigrator</c>'s production bootstrap command) — same caller owns
    /// <c>IUnitOfWork.SaveChangesAsync</c> afterward.
    /// </summary>
    Task SeedAsync(Guid tenantId, DateTimeOffset nowUtc, CancellationToken cancellationToken);
}
