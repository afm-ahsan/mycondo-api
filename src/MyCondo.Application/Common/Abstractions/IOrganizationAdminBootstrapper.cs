using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Provisions a tenant-wide <c>OrganizationAdmin</c> system role (every non-Platform catalogue
/// permission), grants it, and assigns it to the given user. Used by both
/// <c>RegisterUserCommandHandler</c> (first user of a tenant registering themselves) and the
/// <c>MyCondo.DbMigrator</c> tool's production bootstrap command (ADR-015) — the same sequence, two
/// different entry points, so it lives here instead of being duplicated. Adds to the caller's current
/// unit of work; the caller owns calling <c>IUnitOfWork.SaveChangesAsync</c> afterward.
///
/// Phase 2 (mycondo-docs ADR-020) replaces the legacy tenant <c>SuperAdmin</c> role — which granted the
/// *entire* permission catalogue, including the <c>platform.*</c> permissions Phase 1 added to the same
/// shared table — with <c>OrganizationAdmin</c>, which excludes them. This interface used to be named
/// <c>ISuperAdminBootstrapper</c>; existing tenants that already have a legacy <c>SuperAdmin</c> role
/// and assignment are left completely untouched (never renamed, never revoked) — only tenants
/// bootstrapped from here on get <c>OrganizationAdmin</c> instead.
/// </summary>
public interface IOrganizationAdminBootstrapper
{
    Task BootstrapAsync(Guid tenantId, User user, DateTimeOffset nowUtc, CancellationToken cancellationToken);

    /// <summary>
    /// Reconciles an *already-bootstrapped* tenant's existing OrganizationAdmin role (found by
    /// <c>Role.Code == "organization.admin"</c>) against the current permission catalogue — grants any
    /// non-Platform/non-tenant-lifecycle permission the role doesn't already have, same blanket-grant
    /// rule as <see cref="BootstrapAsync"/>, but additive-only (never revokes) so it's safe to call
    /// repeatedly. Needed because <see cref="BootstrapAsync"/> only ever runs once, at a tenant's
    /// first-user registration — a permission added to the catalogue afterward (e.g. a later feature's
    /// new permission) would otherwise never reach that tenant's OrganizationAdmin. A tenant with no
    /// OrganizationAdmin role (not yet bootstrapped, or still on the legacy tenant <c>SuperAdmin</c>
    /// role predating Phase 2 — see this interface's own doc comment) is a no-op, not an error: legacy
    /// SuperAdmin tenants are left untouched, same as <see cref="BootstrapAsync"/>'s policy. Returns the
    /// number of grants created, for caller logging. Adds to the caller's current unit of work; the
    /// caller owns calling <c>IUnitOfWork.SaveChangesAsync</c> afterward.
    /// </summary>
    Task<int> ReconcilePermissionsAsync(Guid tenantId, DateTimeOffset nowUtc, CancellationToken cancellationToken);
}
