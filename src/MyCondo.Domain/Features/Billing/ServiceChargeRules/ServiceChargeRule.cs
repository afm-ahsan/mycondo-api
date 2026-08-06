using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Billing.ServiceChargeRules.Exceptions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Billing.ServiceChargeRules;

/// <summary>
/// A building-scoped rate card entry — immutable once created. To change an amount, method,
/// unit-type filter, or category, end this rule's effective period (<see cref="EndEffectivePeriod"/>)
/// and create a new one; there is no in-place edit, which is what keeps historical
/// <see cref="Invoices.InvoiceLine"/> snapshots trustworthy. Scoped strictly at the Building level —
/// there is no Property/Tower/Flat-level rule in this slice (Tower/Floor aggregates don't exist yet;
/// a flat-level override would need a second nullable scope dimension with precedence rules, which
/// this design deliberately avoids).
/// </summary>
public sealed class ServiceChargeRule : AggregateRoot<ServiceChargeRuleId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public BuildingId BuildingId { get; private set; }
    public string Category { get; private set; }
    public string Name { get; private set; }
    public CalculationMethod CalculationMethod { get; private set; }
    public decimal Rate { get; private set; }
    public FlatType? UnitTypeFilter { get; private set; }
    public BillingFrequency Frequency { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private ServiceChargeRule()
    {
        Category = null!;
        Name = null!;
    }

    private ServiceChargeRule(
        ServiceChargeRuleId id,
        Guid tenantId,
        BuildingId buildingId,
        string category,
        string name,
        CalculationMethod calculationMethod,
        decimal rate,
        FlatType? unitTypeFilter,
        BillingFrequency frequency,
        DateOnly effectiveFrom,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        BuildingId = buildingId;
        Category = category;
        Name = name;
        CalculationMethod = calculationMethod;
        Rate = rate;
        UnitTypeFilter = unitTypeFilter;
        Frequency = frequency;
        EffectiveFrom = effectiveFrom;
        IsActive = true;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static ServiceChargeRule Create(
        Guid tenantId,
        BuildingId buildingId,
        string category,
        string name,
        CalculationMethod calculationMethod,
        decimal rate,
        FlatType? unitTypeFilter,
        BillingFrequency frequency,
        DateOnly effectiveFrom,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (rate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rate), "Rate must be positive.");
        }

        return new ServiceChargeRule(
            ServiceChargeRuleId.New(), tenantId, buildingId, category.Trim(), name.Trim(), calculationMethod, rate,
            unitTypeFilter, frequency, effectiveFrom, nowUtc);
    }

    /// <summary>Ends this rule's effective period — the only way to retire a rule short of a hard
    /// <see cref="Deactivate"/>. A successor rule can start immediately after.</summary>
    public void EndEffectivePeriod(DateOnly effectiveTo)
    {
        if (EffectiveTo is not null)
        {
            throw new ServiceChargeRuleAlreadyEndedException(Id);
        }

        if (effectiveTo < EffectiveFrom)
        {
            throw new ArgumentOutOfRangeException(nameof(effectiveTo), "EffectiveTo cannot precede EffectiveFrom.");
        }

        EffectiveTo = effectiveTo;
        Version++;
    }

    /// <summary>Administrative kill switch, independent of effective dates — for correcting a rule
    /// entered in error without waiting for/backdating an effective-period end.</summary>
    public void Deactivate()
    {
        IsActive = false;
        Version++;
    }

    /// <summary>True only if this rule covers the billing period in full — partial-period overlap
    /// does not apply (proration is out of scope for this slice).</summary>
    public bool AppliesToPeriod(DateOnly periodStart, DateOnly periodEnd) =>
        IsActive && EffectiveFrom <= periodStart && (EffectiveTo is null || EffectiveTo >= periodEnd);
}
