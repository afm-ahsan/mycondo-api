using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;

namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// The Subscription lifecycle access gate (ADR-032 §5/§6/§14's <c>SubscriptionLifecycleAllowsAccess</c>
/// predicate — the extension point ADR-033 §14/§16 reserved without building). Answers only the coarse,
/// whole-tier question; never degrades <see cref="ITenantEntitlementService"/>'s per-feature answer
/// (ADR-033 §14 — that predicate never reads <c>OrganizationSubscription.Status</c> beyond existence).
/// </summary>
public interface ISubscriptionLifecycleAccessService
{
    /// <summary>The tenant's current <see cref="TenantAccessMode"/> and, when <see cref="TenantAccessMode.ReadOnly"/>,
    /// the <see cref="OrganizationSubscriptionStatus"/> that produced it (for the caller to build a
    /// distinguishing error code). <c>SourceStatus</c> is <c>null</c> when the mode is
    /// <see cref="TenantAccessMode.Full"/>, including the "no subscription has ever been provisioned for
    /// this tenant" case (ADR-032 Task 10 §21/§77; retained per ADR-033 Task 12C — see
    /// <see cref="SubscriptionLifecyclePolicy.Evaluate"/> for why a missing subscription still must not be
    /// treated as a lockout).</summary>
    Task<SubscriptionLifecycleAccess> GetAccessAsync(Guid tenantId, CancellationToken cancellationToken);
}

/// <summary>Result of <see cref="ISubscriptionLifecycleAccessService.GetAccessAsync"/> — pairs the coarse
/// <see cref="TenantAccessMode"/> decision with the subscription status that produced it, so callers can
/// build a distinguishing lifecycle error code without a second lookup.</summary>
public readonly record struct SubscriptionLifecycleAccess(TenantAccessMode Mode, OrganizationSubscriptionStatus? SourceStatus)
{
    public static SubscriptionLifecycleAccess Full { get; } = new(TenantAccessMode.Full, null);

    public static SubscriptionLifecycleAccess ReadOnly(OrganizationSubscriptionStatus sourceStatus) =>
        new(TenantAccessMode.ReadOnly, sourceStatus);
}

/// <summary>
/// Pure, side-effect-free Subscription lifecycle policy (ADR-032 §4/§5/§14) — separated from state
/// retrieval per ADR-032 Task 10 §50. Deliberately does not see <c>OrganizationSubscription</c> at all;
/// it only classifies an already-resolved status (or its absence), so it stays exhaustively unit-testable.
/// </summary>
public static class SubscriptionLifecyclePolicy
{
    /// <summary>Classifies a resolved subscription status. <paramref name="status"/> is <c>null</c> only
    /// for "no subscription row exists for this tenant at all" (never for Active/PastDue/Restricted, which
    /// <see cref="IOrganizationSubscriptionRepository.GetCurrentForTenantAsync"/> always returns when
    /// present) — originally ADR-032 Task 10 §21 for "no subscription-aware provisioning path exists yet."
    ///
    /// <para><b>Retained (ADR-033 Task 12C):</b> subscription-aware provisioning now exists
    /// (<c>ProvisionOrganizationWithAdminCommandHandler</c>, Task 12A) and creates every new tenant's
    /// subscription atomically, so a successfully provisioned tenant is never null here. This fallback is
    /// still reachable, though: <c>LegacyMigrationSubscriptionBackfillSeeder</c>'s per-tenant failure
    /// isolation and its <c>comparison.HasLoss</c> safety net can each leave a legacy tenant unmigrated —
    /// and therefore null here — for an unbounded window until backfill succeeds for it. Task 12C evaluated
    /// removing this fallback and retained it for exactly that category: doing so today would turn a
    /// transient backfill failure into a hard lockout for an otherwise-legitimate, previously operational
    /// tenant rather than the current degraded-but-available state. Revisit only once that failure/rejection
    /// window is itself closed (or made provably unreachable).</para></summary>
    public static SubscriptionLifecycleAccess Evaluate(OrganizationSubscriptionStatus? status) => status switch
    {
        null => SubscriptionLifecycleAccess.Full,
        OrganizationSubscriptionStatus.Active => SubscriptionLifecycleAccess.Full,
        OrganizationSubscriptionStatus.PastDue => SubscriptionLifecycleAccess.Full,
        OrganizationSubscriptionStatus.Restricted => SubscriptionLifecycleAccess.ReadOnly(OrganizationSubscriptionStatus.Restricted),
        OrganizationSubscriptionStatus.Expired => SubscriptionLifecycleAccess.ReadOnly(OrganizationSubscriptionStatus.Expired),
        OrganizationSubscriptionStatus.Canceled => SubscriptionLifecycleAccess.ReadOnly(OrganizationSubscriptionStatus.Canceled),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unrecognized OrganizationSubscriptionStatus.")
    };
}
