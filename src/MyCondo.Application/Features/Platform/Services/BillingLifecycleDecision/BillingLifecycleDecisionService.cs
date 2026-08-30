using MyCondo.Application.Common;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;

namespace MyCondo.Application.Features.Platform.Services.BillingLifecycleDecision;

/// <summary>Production implementation of <see cref="IBillingLifecycleDecisionService"/> — implements
/// the decision matrix recorded in ADR-034 Task 14E.0 §3 exactly, with no independently-derived
/// thresholds. Every <c>OrganizationSubscriptionStatus</c> case is enumerated explicitly (no default
/// fall-through), so a future new status forces this decision to be revisited rather than silently
/// defaulting to <see cref="BillingLifecycleRecommendedAction.NoAction"/>.</summary>
public sealed class BillingLifecycleDecisionService : IBillingLifecycleDecisionService
{
    public BillingLifecycleDecisionResult Evaluate(
        OrganizationSubscription subscription,
        IReadOnlyList<SubscriptionInvoice> invoices,
        DateOnly businessDate)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentNullException.ThrowIfNull(invoices);

        List<int> overdueDays = invoices
            .Select(invoice => SubscriptionInvoiceOverdueCalculator.DaysOverdue(invoice, businessDate))
            .Where(days => days is not null)
            .Select(days => days!.Value)
            .ToList();

        bool hasOverdue = overdueDays.Count > 0;
        int maxDaysOverdue = hasOverdue ? overdueDays.Max() : 0;

        (BillingLifecycleRecommendedAction action, string reason) =
            Decide(subscription.Status, hasOverdue, maxDaysOverdue);

        return new BillingLifecycleDecisionResult(
            subscription.TenantId, subscription.Status, action, hasOverdue, maxDaysOverdue, reason);
    }

    private static (BillingLifecycleRecommendedAction Action, string Reason) Decide(
        OrganizationSubscriptionStatus status, bool hasOverdue, int maxDaysOverdue) => status switch
    {
        OrganizationSubscriptionStatus.Active => hasOverdue
            ? (BillingLifecycleRecommendedAction.MarkPastDue,
                $"Active subscription has a collectible invoice {maxDaysOverdue} day(s) overdue.")
            : (BillingLifecycleRecommendedAction.NoAction,
                "Active subscription has no overdue collectible invoice."),

        OrganizationSubscriptionStatus.PastDue => !hasOverdue
            ? (BillingLifecycleRecommendedAction.Reactivate,
                "PastDue subscription has no remaining overdue collectible invoice.")
            : maxDaysOverdue >= 30
                ? (BillingLifecycleRecommendedAction.Restrict,
                    $"PastDue subscription reached {maxDaysOverdue} day(s) overdue (>= 30-day threshold).")
                : (BillingLifecycleRecommendedAction.NoAction,
                    $"PastDue subscription is {maxDaysOverdue} day(s) overdue, within the grace window."),

        OrganizationSubscriptionStatus.Restricted => maxDaysOverdue >= 60
            ? (BillingLifecycleRecommendedAction.Expire,
                $"Restricted subscription reached {maxDaysOverdue} day(s) overdue (>= 60-day threshold).")
            : (BillingLifecycleRecommendedAction.NoAction,
                $"Restricted subscription is {maxDaysOverdue} day(s) overdue, below the 60-day expiry threshold."),

        OrganizationSubscriptionStatus.Expired =>
            (BillingLifecycleRecommendedAction.NoAction, "Expired subscriptions are never automatically resurrected."),

        OrganizationSubscriptionStatus.Canceled =>
            (BillingLifecycleRecommendedAction.NoAction,
                "Cancellation remains an explicit commercial decision independent of payment status."),

        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unhandled OrganizationSubscriptionStatus.")
    };
}
