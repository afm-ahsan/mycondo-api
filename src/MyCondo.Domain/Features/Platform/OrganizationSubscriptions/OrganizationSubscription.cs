using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions.Exceptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Domain.Features.Platform.OrganizationSubscriptions;

/// <summary>
/// The commercial agreement between CondoBD Platform and one organization (ADR-032 §4, ADR-033 §10) —
/// distinct from <c>Tenant</c> (organizational identity/lifecycle), <c>SubscriptionPackage</c>/
/// <c>SubscriptionPackageVersion</c> (what is being sold), and any future subscription invoice (what
/// was actually billed). Lives in the <c>platform</c> schema, no FK to <c>tenancy.tenants</c> — same
/// no-FK convention as <c>TenantModule</c>/<c>PlatformAuditLogEntry.TenantId</c>.
///
/// <para><b>Commercial snapshot (ADR-033 §11):</b> <see cref="BasePrice"/>, <see cref="Discount"/>,
/// <see cref="EffectivePrice"/>, and <see cref="Currency"/> are captured at creation/change time and
/// never re-derived from the live <see cref="SubscriptionPackageVersion"/> afterward — see
/// <see cref="OrganizationSubscriptionCommercialTerms"/> for how the starting <see cref="BasePrice"/>
/// is resolved. <see cref="EffectivePrice"/> = <see cref="BasePrice"/> − <see cref="Discount"/> is
/// enforced centrally here, never accepted as an independent caller-supplied value.</para>
///
/// <para><b>One current subscription per tenant</b> (Active/PastDue/Restricted) is enforced by a
/// partial unique database index, not by this type — see <c>OrganizationSubscriptionConfiguration</c>.</para>
/// </summary>
public sealed class OrganizationSubscription : AggregateRoot<OrganizationSubscriptionId>
{
    public Guid TenantId { get; private set; }
    public SubscriptionPackageVersionId PackageVersionId { get; private set; }

    public OrganizationSubscriptionStatus Status { get; private set; }
    public BillingCycle BillingCycle { get; private set; }

    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public DateOnly? NextBillingDate { get; private set; }

    public decimal BasePrice { get; private set; }
    public decimal Discount { get; private set; }
    public decimal EffectivePrice { get; private set; }
    public string Currency { get; private set; }

    public DateTimeOffset ActivatedAt { get; private set; }
    public DateTimeOffset? RestrictedAt { get; private set; }
    public DateTimeOffset? ExpiredAt { get; private set; }
    public DateTimeOffset? CanceledAt { get; private set; }

    public bool AutoRenew { get; private set; }

    private OrganizationSubscription()
    {
        Currency = null!;
    }

    private OrganizationSubscription(
        OrganizationSubscriptionId id,
        Guid tenantId,
        SubscriptionPackageVersionId packageVersionId,
        BillingCycle billingCycle,
        DateOnly startDate,
        DateOnly? endDate,
        DateOnly? nextBillingDate,
        decimal basePrice,
        decimal discount,
        decimal effectivePrice,
        string currency,
        DateTimeOffset activatedAtUtc,
        bool autoRenew) : base(id)
    {
        TenantId = tenantId;
        PackageVersionId = packageVersionId;
        Status = OrganizationSubscriptionStatus.Active;
        BillingCycle = billingCycle;
        StartDate = startDate;
        EndDate = endDate;
        NextBillingDate = nextBillingDate;
        BasePrice = basePrice;
        Discount = discount;
        EffectivePrice = effectivePrice;
        Currency = currency;
        ActivatedAt = activatedAtUtc;
        AutoRenew = autoRenew;
    }

    /// <summary>
    /// Creates a new subscription in the <see cref="OrganizationSubscriptionStatus.Active"/> state.
    /// <paramref name="basePrice"/> must already have been resolved against the assigned package
    /// version's billing-cycle prices via <see cref="OrganizationSubscriptionCommercialTerms.ResolveBasePrice"/>
    /// — this factory does not re-validate that the package version actually offers
    /// <paramref name="billingCycle"/>, only the resulting commercial values.
    /// </summary>
    public static OrganizationSubscription Create(
        Guid tenantId,
        SubscriptionPackageVersionId packageVersionId,
        BillingCycle billingCycle,
        DateOnly startDate,
        DateOnly? endDate,
        DateOnly? nextBillingDate,
        decimal basePrice,
        decimal discount,
        string currency,
        DateTimeOffset activatedAtUtc,
        bool autoRenew)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (basePrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(basePrice), "BasePrice cannot be negative.");
        }

        if (discount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(discount), "Discount cannot be negative.");
        }

        decimal effectivePrice = basePrice - discount;
        if (effectivePrice < 0)
        {
            throw new ArgumentException("Discount cannot exceed BasePrice.", nameof(discount));
        }

        if (endDate is not null && endDate < startDate)
        {
            throw new ArgumentException("EndDate must not be before StartDate.", nameof(endDate));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        return new OrganizationSubscription(
            OrganizationSubscriptionId.New(), tenantId, packageVersionId, billingCycle, startDate, endDate,
            nextBillingDate, basePrice, discount, effectivePrice, currency.Trim(), activatedAtUtc, autoRenew);
    }

    /// <summary>Active → PastDue — payment overdue, grace window open. Full operational access is
    /// retained at the application layer (ADR-032 §5); this method only records the lifecycle fact.</summary>
    public void MarkPastDue()
    {
        if (Status != OrganizationSubscriptionStatus.Active)
        {
            throw new OrganizationSubscriptionInvalidTransitionException(Id, Status, OrganizationSubscriptionStatus.PastDue);
        }

        Status = OrganizationSubscriptionStatus.PastDue;
    }

    /// <summary>PastDue → Active — payment resolved before escalation to <see cref="Restrict"/>.</summary>
    public void Reactivate()
    {
        if (Status != OrganizationSubscriptionStatus.PastDue)
        {
            throw new OrganizationSubscriptionInvalidTransitionException(Id, Status, OrganizationSubscriptionStatus.Active);
        }

        Status = OrganizationSubscriptionStatus.Active;
    }

    /// <summary>PastDue → Restricted — escalated non-payment, past the grace window.</summary>
    public void Restrict(DateTimeOffset restrictedAtUtc)
    {
        if (Status != OrganizationSubscriptionStatus.PastDue)
        {
            throw new OrganizationSubscriptionInvalidTransitionException(Id, Status, OrganizationSubscriptionStatus.Restricted);
        }

        Status = OrganizationSubscriptionStatus.Restricted;
        RestrictedAt = restrictedAtUtc;
    }

    /// <summary>Active or PastDue → Canceled — customer-initiated non-renewal, effective at the
    /// current billing period's end (the caller is responsible for setting <see cref="EndDate"/>
    /// accordingly before/around this call; this method only records the lifecycle fact).</summary>
    public void Cancel(DateTimeOffset canceledAtUtc)
    {
        if (Status != OrganizationSubscriptionStatus.Active && Status != OrganizationSubscriptionStatus.PastDue)
        {
            throw new OrganizationSubscriptionInvalidTransitionException(Id, Status, OrganizationSubscriptionStatus.Canceled);
        }

        Status = OrganizationSubscriptionStatus.Canceled;
        CanceledAt = canceledAtUtc;
    }

    /// <summary>Restricted or Canceled → Expired — terminal. The commercial relationship has lapsed;
    /// the organization shell may still exist and could in principle resubscribe (ADR-032 §4).</summary>
    public void Expire(DateTimeOffset expiredAtUtc)
    {
        if (Status != OrganizationSubscriptionStatus.Restricted && Status != OrganizationSubscriptionStatus.Canceled)
        {
            throw new OrganizationSubscriptionInvalidTransitionException(Id, Status, OrganizationSubscriptionStatus.Expired);
        }

        Status = OrganizationSubscriptionStatus.Expired;
        ExpiredAt = expiredAtUtc;
    }
}
