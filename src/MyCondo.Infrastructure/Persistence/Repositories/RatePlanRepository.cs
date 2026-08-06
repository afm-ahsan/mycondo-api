using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.RatePlans;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class RatePlanRepository(MyCondoDbContext db) : IRatePlanRepository
{
    public Task<RatePlan?> GetByIdAsync(RatePlanId id, CancellationToken cancellationToken) =>
        db.Set<RatePlan>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<PagedResult<RatePlan>> SearchAsync(
        Guid tenantId, BuildingId buildingId, UtilityType? utilityType, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<RatePlan> query = db.Set<RatePlan>()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.BuildingId == buildingId);

        if (utilityType is not null)
        {
            query = query.Where(p => p.UtilityType == utilityType);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<RatePlan> items = await query
            .OrderByDescending(p => p.EffectiveFrom)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<RatePlan>(items, page, pageSize, total);
    }

    public Task<RatePlan?> GetApplicablePlanAsync(
        Guid tenantId, BuildingId buildingId, UtilityType utilityType, DateOnly periodStart, DateOnly periodEnd,
        CancellationToken cancellationToken) =>
        db.Set<RatePlan>()
            .AsNoTracking()
            .Where(p =>
                p.TenantId == tenantId &&
                p.BuildingId == buildingId &&
                p.UtilityType == utilityType &&
                p.IsActive &&
                p.EffectiveFrom <= periodStart &&
                (p.EffectiveTo == null || p.EffectiveTo >= periodEnd))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> HasOverlappingPlanAsync(
        Guid tenantId, BuildingId buildingId, UtilityType utilityType, DateOnly effectiveFrom, DateOnly? effectiveTo,
        CancellationToken cancellationToken)
    {
        DateOnly newRangeEnd = effectiveTo ?? DateOnly.MaxValue;

        return await db.Set<RatePlan>()
            .AsNoTracking()
            .Where(p =>
                p.TenantId == tenantId &&
                p.BuildingId == buildingId &&
                p.UtilityType == utilityType &&
                p.EffectiveFrom <= newRangeEnd &&
                (p.EffectiveTo == null ? DateOnly.MaxValue : p.EffectiveTo.Value) >= effectiveFrom)
            .AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RateSlab>> GetSlabsForPlanAsync(RatePlanId ratePlanId, CancellationToken cancellationToken) =>
        await db.Set<RateSlab>()
            .AsNoTracking()
            .Where(s => s.RatePlanId == ratePlanId)
            .OrderBy(s => s.SlabOrder)
            .ToListAsync(cancellationToken);

    public void Add(RatePlan plan) => db.Set<RatePlan>().Add(plan);

    public void AddSlabs(IEnumerable<RateSlab> slabs) => db.Set<RateSlab>().AddRange(slabs);
}
