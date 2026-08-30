using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Domain.Features.Platform.SubscriptionInvoices;

/// <summary>
/// The one charge explaining a <see cref="SubscriptionInvoice"/> — every value is a commercial-snapshot
/// copy taken at issuance (<see cref="SubscriptionInvoice.Issue"/>), preserving package/version/pricing
/// provenance exactly as <see cref="OrganizationSubscription"/>'s own commercial snapshot does. A line
/// never reads back through <see cref="PackageVersionId"/> to the live package version — a later price
/// or package-name change cannot alter an already-issued charge. Only created via
/// <see cref="SubscriptionInvoice.Issue"/>; no public constructor bypasses that.
/// </summary>
public sealed class SubscriptionInvoiceLine : Entity<SubscriptionInvoiceLineId>
{
    public SubscriptionInvoiceId SubscriptionInvoiceId { get; private set; }
    public SubscriptionPackageVersionId PackageVersionId { get; private set; }
    public string PackageNameSnapshot { get; private set; }
    public int PackageVersionNumberSnapshot { get; private set; }
    public BillingCycle BillingCycleSnapshot { get; private set; }
    public decimal BasePriceSnapshot { get; private set; }
    public decimal DiscountSnapshot { get; private set; }
    public decimal LineAmount { get; private set; }
    public string Description { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private SubscriptionInvoiceLine()
    {
        PackageNameSnapshot = null!;
        Description = null!;
    }

    internal SubscriptionInvoiceLine(
        SubscriptionInvoiceLineId id,
        SubscriptionInvoiceId subscriptionInvoiceId,
        SubscriptionInvoiceLineInput input,
        DateTimeOffset nowUtc) : base(id)
    {
        SubscriptionInvoiceId = subscriptionInvoiceId;
        PackageVersionId = input.PackageVersionId;
        PackageNameSnapshot = input.PackageNameSnapshot;
        PackageVersionNumberSnapshot = input.PackageVersionNumberSnapshot;
        BillingCycleSnapshot = input.BillingCycleSnapshot;
        BasePriceSnapshot = input.BasePriceSnapshot;
        DiscountSnapshot = input.DiscountSnapshot;
        LineAmount = input.LineAmount;
        Description = input.Description;
        CreatedAtUtc = nowUtc;
    }
}
