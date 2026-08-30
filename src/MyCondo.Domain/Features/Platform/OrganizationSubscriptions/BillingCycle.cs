namespace MyCondo.Domain.Features.Platform.OrganizationSubscriptions;

/// <summary>
/// The subscription's billing-cycle choice (ADR-033 §10) — must be supported by the assigned
/// <c>SubscriptionPackageVersion</c> (i.e. that cycle's price column must not be null), enforced by
/// <see cref="OrganizationSubscriptionCommercialTerms.ResolveBasePrice"/> before construction.
/// </summary>
public enum BillingCycle
{
    Monthly = 0,
    Quarterly = 1,
    SemiAnnual = 2,
    Annual = 3
}
