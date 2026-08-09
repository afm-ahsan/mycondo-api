namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Seeds a tenant's two resident-facing condominium-scoped system roles — FlatOwner and Tenant (Phase
/// 3, mycondo-docs ADR-021). Like <see cref="ICondominiumRoleCatalogueSeeder"/>'s staff roles, these are
/// <c>Role.CreateSystem</c> roles with <c>RequiresBuildingScope = true</c>; unlike staff roles, holding
/// one grants nothing by itself — every permission it carries (ownership.view/lease.view/
/// invoice.view.own) is inert without a matching active FlatOwnership/occupancy relationship (see
/// IFlatAccessAuthorizer). Nothing is auto-assigned to any user by this seeder; assigning FlatOwner/
/// Tenant to a specific resident, for a specific Building, remains an explicit admin action via the
/// existing role-assignment endpoint.
/// </summary>
public interface IResidentRoleCatalogueSeeder
{
    Task SeedAsync(Guid tenantId, DateTimeOffset nowUtc, CancellationToken cancellationToken);
}
