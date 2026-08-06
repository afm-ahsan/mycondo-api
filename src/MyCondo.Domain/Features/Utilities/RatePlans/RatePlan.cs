using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.RatePlans.Exceptions;

namespace MyCondo.Domain.Features.Utilities.RatePlans;

/// <summary>
/// A building-scoped, utility-type-scoped rate card — immutable once created, effective-dated, same
/// pattern as <c>Billing.ServiceChargeRules.ServiceChargeRule</c> exactly (see that type's doc comment
/// for the full rationale). To change a rate/slab/structure, end this plan's effective period and
/// create a new one.
/// </summary>
public sealed class RatePlan : AggregateRoot<RatePlanId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public BuildingId BuildingId { get; private set; }
    public UtilityType UtilityType { get; private set; }
    public string Name { get; private set; }
    public RateStructure Structure { get; private set; }
    public decimal? FixedAmount { get; private set; }
    public decimal FixedServiceCharge { get; private set; }
    public decimal TaxPercentage { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private RatePlan()
    {
        Name = null!;
    }

    private RatePlan(
        RatePlanId id,
        Guid tenantId,
        BuildingId buildingId,
        UtilityType utilityType,
        string name,
        RateStructure structure,
        decimal? fixedAmount,
        decimal fixedServiceCharge,
        decimal taxPercentage,
        DateOnly effectiveFrom,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        BuildingId = buildingId;
        UtilityType = utilityType;
        Name = name;
        Structure = structure;
        FixedAmount = fixedAmount;
        FixedServiceCharge = fixedServiceCharge;
        TaxPercentage = taxPercentage;
        EffectiveFrom = effectiveFrom;
        IsActive = true;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    /// <summary>Validates the slab set (contiguous, ordered, only the last slab unbounded) when
    /// <paramref name="structure"/> is <see cref="RateStructure.Metered"/>; <paramref name="slabs"/>
    /// must be empty when <see cref="RateStructure.Fixed"/> — the two are mutually exclusive.</summary>
    public static (RatePlan Plan, IReadOnlyList<RateSlab> Slabs) Create(
        Guid tenantId,
        BuildingId buildingId,
        UtilityType utilityType,
        string name,
        RateStructure structure,
        decimal? fixedAmount,
        decimal fixedServiceCharge,
        decimal taxPercentage,
        DateOnly effectiveFrom,
        IReadOnlyList<RateSlabInput> slabs,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (fixedServiceCharge < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fixedServiceCharge), "FixedServiceCharge cannot be negative.");
        }

        if (taxPercentage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(taxPercentage), "TaxPercentage cannot be negative.");
        }

        if (structure == RateStructure.Fixed)
        {
            if (fixedAmount is not > 0)
            {
                throw new ArgumentException("FixedAmount must be positive when Structure is Fixed.", nameof(fixedAmount));
            }

            if (slabs.Count > 0)
            {
                throw new ArgumentException("A Fixed-structure rate plan must not declare slabs.", nameof(slabs));
            }
        }
        else
        {
            if (fixedAmount is not null)
            {
                throw new ArgumentException("FixedAmount must be null when Structure is Metered.", nameof(fixedAmount));
            }

            ValidateSlabs(slabs);
        }

        RatePlanId planId = RatePlanId.New();
        RatePlan plan = new(
            planId, tenantId, buildingId, utilityType, name.Trim(), structure, fixedAmount, fixedServiceCharge,
            taxPercentage, effectiveFrom, nowUtc);

        List<RateSlab> rateSlabs = slabs
            .Select(input => new RateSlab(RateSlabId.New(), tenantId, planId, input))
            .ToList();

        return (plan, rateSlabs);
    }

    private static void ValidateSlabs(IReadOnlyList<RateSlabInput> slabs)
    {
        if (slabs.Count == 0)
        {
            throw new ArgumentException("A Metered-structure rate plan needs at least one slab.", nameof(slabs));
        }

        List<RateSlabInput> ordered = slabs.OrderBy(s => s.SlabOrder).ToList();

        if (ordered[0].SlabOrder != 1 || ordered[0].FromUnits != 0)
        {
            throw new ArgumentException("The first slab must have SlabOrder 1 and FromUnits 0.", nameof(slabs));
        }

        for (int i = 0; i < ordered.Count; i++)
        {
            RateSlabInput slab = ordered[i];

            if (slab.SlabOrder != i + 1)
            {
                throw new ArgumentException("Slab orders must be sequential starting at 1.", nameof(slabs));
            }

            if (slab.RatePerUnit <= 0)
            {
                throw new ArgumentException("Every slab's RatePerUnit must be positive.", nameof(slabs));
            }

            bool isLast = i == ordered.Count - 1;
            if (isLast != (slab.ToUnits is null))
            {
                throw new ArgumentException("Only the last slab may have a null ToUnits (unbounded).", nameof(slabs));
            }

            if (!isLast)
            {
                if (slab.ToUnits <= slab.FromUnits)
                {
                    throw new ArgumentException("A slab's ToUnits must exceed its FromUnits.", nameof(slabs));
                }

                if (ordered[i + 1].FromUnits != slab.ToUnits)
                {
                    throw new ArgumentException("Slabs must be contiguous — the next slab's FromUnits must equal this slab's ToUnits.", nameof(slabs));
                }
            }
        }
    }

    public void EndEffectivePeriod(DateOnly effectiveTo)
    {
        if (EffectiveTo is not null)
        {
            throw new RatePlanAlreadyEndedException(Id);
        }

        if (effectiveTo < EffectiveFrom)
        {
            throw new ArgumentOutOfRangeException(nameof(effectiveTo), "EffectiveTo cannot precede EffectiveFrom.");
        }

        EffectiveTo = effectiveTo;
        Version++;
    }

    public void Deactivate()
    {
        IsActive = false;
        Version++;
    }

    public bool AppliesToPeriod(DateOnly periodStart, DateOnly periodEnd) =>
        IsActive && EffectiveFrom <= periodStart && (EffectiveTo is null || EffectiveTo >= periodEnd);
}
