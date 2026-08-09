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
}
