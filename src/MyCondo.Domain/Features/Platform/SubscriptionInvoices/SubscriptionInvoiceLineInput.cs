using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Domain.Features.Platform.SubscriptionInvoices;

/// <summary>
/// Unpersisted input to <see cref="SubscriptionInvoice.Issue"/> — the caller has already resolved the
/// billed <see cref="OrganizationSubscription"/>'s commercial snapshot for the period being invoiced;
/// this record carries that snapshot so <see cref="SubscriptionInvoice.Issue"/> can materialize a
/// <see cref="SubscriptionInvoiceLine"/> without re-deriving anything from the (possibly since-changed)
/// live subscription or package version.
/// </summary>
public sealed record SubscriptionInvoiceLineInput(
    SubscriptionPackageVersionId PackageVersionId,
    string PackageNameSnapshot,
    int PackageVersionNumberSnapshot,
    BillingCycle BillingCycleSnapshot,
    decimal BasePriceSnapshot,
    decimal DiscountSnapshot,
    decimal LineAmount,
    string Description);
