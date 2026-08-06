using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Utilities.RatePlans;

/// <summary>One tier of a <see cref="RatePlan"/>'s slab structure — only meaningful when
/// <see cref="RatePlan.Structure"/> is <see cref="RateStructure.Metered"/>. Only created via
/// <see cref="RatePlan.Create"/>, which validates the slabs are contiguous and ordered.</summary>
public sealed class RateSlab : Entity<RateSlabId>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public RatePlanId RatePlanId { get; private set; }
    public int SlabOrder { get; private set; }
    public decimal FromUnits { get; private set; }
    public decimal? ToUnits { get; private set; }
    public decimal RatePerUnit { get; private set; }

    private RateSlab() { }

    internal RateSlab(
        RateSlabId id, Guid tenantId, RatePlanId ratePlanId, RateSlabInput input) : base(id)
    {
        TenantId = tenantId;
        RatePlanId = ratePlanId;
        SlabOrder = input.SlabOrder;
        FromUnits = input.FromUnits;
        ToUnits = input.ToUnits;
        RatePerUnit = input.RatePerUnit;
    }
}
