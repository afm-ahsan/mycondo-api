namespace MyCondo.Domain.Features.Tenancy;

public enum TenantStatus
{
    PendingActivation = 0,
    Active = 1,
    Suspended = 2,

    /// <summary>Terminal organization lifecycle state (ADR-032 §4) — permanent decommission, no login
    /// ever again once reached. The only new value ADR-032 approves; <c>Restricted</c>/<c>Frozen</c> were
    /// explicitly rejected as <see cref="TenantStatus"/> values since they describe subscription-driven
    /// access degradation, not an organizational fact (see <c>OrganizationSubscriptionStatus</c>).</summary>
    Closed = 3
}
