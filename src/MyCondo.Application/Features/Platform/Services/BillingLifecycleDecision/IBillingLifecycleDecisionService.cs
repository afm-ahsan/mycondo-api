using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;

namespace MyCondo.Application.Features.Platform.Services.BillingLifecycleDecision;

/// <summary>The next valid subscription-lifecycle transition this decision recommends (ADR-034 Task
/// 14E.0 §3) — never a command: applying it through <see cref="OrganizationSubscription"/>'s own
/// guarded transition methods remains Task 14F's responsibility.</summary>
public enum BillingLifecycleRecommendedAction
{
    NoAction,
    MarkPastDue,
    Restrict,
    Expire,
    Reactivate
}

/// <summary>The deterministic outcome of evaluating one organization's billing facts against its
/// current <see cref="OrganizationSubscriptionStatus"/> (ADR-034 Task 14E.0 §3). Carries enough context
/// for Task 14F to decide whether to apply <see cref="RecommendedAction"/> without recomputing anything
/// here itself.</summary>
public sealed record BillingLifecycleDecisionResult(
    Guid TenantId,
    OrganizationSubscriptionStatus CurrentStatus,
    BillingLifecycleRecommendedAction RecommendedAction,
    bool HasOverdueCollectibleInvoice,
    int MaxDaysOverdue,
    string Reason);

/// <summary>
/// Pure, deterministic billing-to-lifecycle recommendation service (ADR-034 Task 14E). Reads the
/// current <see cref="OrganizationSubscription"/> status and its collectible invoices' overdue ageing
/// (via <see cref="Application.Common.SubscriptionInvoiceOverdueCalculator"/>, the same calculation
/// Task 14D's outstanding-dues read model uses) and returns a recommendation only — it never calls a
/// lifecycle transition method, persists anything, or mutates its inputs. Structurally separate from
/// <see cref="Common.Services.SubscriptionLifecycleAccessService"/> (entitlement/access resolution) and
/// <c>TenantLifecycleBehavior</c> (pipeline enforcement) — this service answers a different question
/// ("what should change") than either of those ("what is currently allowed").
/// </summary>
public interface IBillingLifecycleDecisionService
{
    /// <summary><paramref name="invoices"/> may be any set of invoices for the organization (including
    /// non-collectible/non-overdue ones) — collectibility and overdue ageing are both derived internally
    /// per invoice, never assumed of the caller. <paramref name="businessDate"/> must be the caller's
    /// already-resolved Asia/Dhaka business date (this service never derives "today" itself, matching
    /// <see cref="ISubscriptionInvoiceRepository.SearchAsync"/>'s convention).</summary>
    BillingLifecycleDecisionResult Evaluate(
        OrganizationSubscription subscription,
        IReadOnlyList<SubscriptionInvoice> invoices,
        DateOnly businessDate);
}
