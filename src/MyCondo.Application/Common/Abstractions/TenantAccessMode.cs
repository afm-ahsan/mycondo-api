namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// The coarse, whole-tier subscription access gate (ADR-032 §5/§6's <c>SubscriptionLifecycleAllowsAccess</c>
/// predicate — named as a reserved extension point by ADR-033 §14/§16) — deliberately separate from, and
/// evaluated before, <c>SubscriptionEntitlementAllowsFeature</c> (<see cref="ITenantEntitlementService"/>).
/// A <see cref="ReadOnly"/> tenant is blocked from every ordinary write regardless of which feature is
/// being touched; this is never conflated with "feature not entitled" (ADR-032 Task 10 §16).
/// </summary>
public enum TenantAccessMode
{
    /// <summary>Active or PastDue subscription — ordinary reads and writes both proceed, subject to
    /// feature entitlement/RBAC/RLS. PastDue retains full access by design (ADR-032 §5) — "overdue
    /// invoice ≠ immediate lockout."</summary>
    Full,

    /// <summary>Restricted, Expired, or Canceled subscription — ordinary reads/exports/reports proceed;
    /// ordinary writes are blocked with a lifecycle-specific denial (never <c>feature_not_entitled</c>).
    /// <see cref="IBillingResolutionOperation"/>-marked requests remain allowed even in this mode.</summary>
    ReadOnly
}
