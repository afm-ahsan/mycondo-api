using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;

namespace MyCondo.Domain.Features.Utilities.RatePlans;

public interface IRatePlanRepository
{
    Task<RatePlan?> GetByIdAsync(RatePlanId id, CancellationToken cancellationToken);

    Task<PagedResult<RatePlan>> SearchAsync(
        Guid tenantId, BuildingId buildingId, UtilityType? utilityType, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>The single rate plan (Metered or Fixed) covering the given period in full — see
    /// <see cref="RatePlan.AppliesToPeriod"/>.</summary>
    Task<RatePlan?> GetApplicablePlanAsync(
        Guid tenantId, BuildingId buildingId, UtilityType utilityType, DateOnly periodStart, DateOnly periodEnd,
        CancellationToken cancellationToken);

    Task<bool> HasOverlappingPlanAsync(
        Guid tenantId, BuildingId buildingId, UtilityType utilityType, DateOnly effectiveFrom, DateOnly? effectiveTo,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RateSlab>> GetSlabsForPlanAsync(RatePlanId ratePlanId, CancellationToken cancellationToken);

    void Add(RatePlan plan);

    void AddSlabs(IEnumerable<RateSlab> slabs);
}
