using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// The Organization lifecycle access gate (ADR-032 §4/§5/§6's <c>OrganizationLifecycleAllowsAccess</c>
/// predicate) — answers only "is this organization's own lifecycle state a hard access boundary," never
/// "does the subscription allow access" (<see cref="ISubscriptionLifecycleAccessService"/>, a separate
/// axis) and never "is this feature entitled" (<see cref="ITenantEntitlementService"/>, a third separate
/// axis). Org-level denial always dominates (ADR-032 §5): evaluated first in <c>TenantLifecycleBehavior</c>,
/// before the subscription axis is even queried.
/// </summary>
public interface ITenantLifecycleAccessService
{
    /// <summary>The tenant's current <see cref="TenantStatus"/>, for the caller to apply
    /// <see cref="OrganizationLifecyclePolicy"/> against. Throws <c>NotFoundException</c> if the tenant does
    /// not exist — a request carrying a <c>TenantId</c> for a tenant that has been deleted/never existed is
    /// a code defect, not a lifecycle state to model.</summary>
    Task<TenantStatus> GetTenantStatusAsync(Guid tenantId, CancellationToken cancellationToken);
}

/// <summary>
/// Pure, side-effect-free Organization lifecycle policy (ADR-032 §4/§5) — separated from state retrieval
/// (<see cref="ITenantLifecycleAccessService"/>) per ADR-032 Task 10 §50, so the access decision itself is
/// trivially unit-testable without a database.
/// </summary>
public static class OrganizationLifecyclePolicy
{
    /// <summary>Only <see cref="TenantStatus.Active"/> allows ordinary tenant application access.
    /// <see cref="TenantStatus.Suspended"/> and <see cref="TenantStatus.Closed"/> are both a full lockout
    /// (ADR-032 §5) — <see cref="TenantStatus.PendingActivation"/> is included defensively (existing login
    /// enforcement already prevents a PendingActivation tenant from ever obtaining a session token, so this
    /// branch is unreachable via a real request today, not a new business rule).</summary>
    public static bool AllowsAccess(TenantStatus status) => status == TenantStatus.Active;
}
