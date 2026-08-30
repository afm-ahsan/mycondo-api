using MyCondo.Domain.Features.Platform.OrganizationSubscriptions.Exceptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Domain.Features.Platform.OrganizationSubscriptions;

/// <summary>
/// Resolves the list-price starting point for a new/changed <see cref="OrganizationSubscription"/>
/// from an already-loaded <see cref="SubscriptionPackageVersion"/> (ADR-033 §9/§11) — the one
/// cross-aggregate check this task's creation invariants require. Mirrors
/// <c>SubscriptionPackageFeatureComposition</c>'s own shape: a pure, static, no-I/O domain service
/// operating on an in-memory aggregate the caller has already loaded.
///
/// The returned price is only ever a <em>starting value</em> copied into
/// <see cref="OrganizationSubscription.BasePrice"/> at creation/change time — never read live again
/// afterward (ADR-033 §11's commercial-snapshot precedence).
/// </summary>
public static class OrganizationSubscriptionCommercialTerms
{
    public static decimal ResolveBasePrice(SubscriptionPackageVersion packageVersion, BillingCycle billingCycle)
    {
        decimal? price = billingCycle switch
        {
            BillingCycle.Monthly => packageVersion.MonthlyPrice,
            BillingCycle.Quarterly => packageVersion.QuarterlyPrice,
            BillingCycle.SemiAnnual => packageVersion.SemiAnnualPrice,
            BillingCycle.Annual => packageVersion.AnnualPrice,
            _ => throw new ArgumentOutOfRangeException(nameof(billingCycle), billingCycle, "Unknown billing cycle.")
        };

        if (price is null)
        {
            throw new UnsupportedBillingCycleException(packageVersion.Id, billingCycle);
        }

        return price.Value;
    }
}
