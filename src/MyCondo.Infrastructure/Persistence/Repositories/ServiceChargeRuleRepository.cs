using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class ServiceChargeRuleRepository(MyCondoDbContext db) : IServiceChargeRuleRepository
{
    public Task<ServiceChargeRule?> GetByIdAsync(ServiceChargeRuleId id, CancellationToken cancellationToken) =>
        db.Set<ServiceChargeRule>().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<PagedResult<ServiceChargeRule>> SearchAsync(
        Guid tenantId, BuildingId buildingId, string? category, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<ServiceChargeRule> query = db.Set<ServiceChargeRule>()
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.BuildingId == buildingId);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(r => r.Category == category);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<ServiceChargeRule> items = await query
            .OrderByDescending(r => r.EffectiveFrom)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ServiceChargeRule>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<ServiceChargeRule>> GetApplicableRulesAsync(
        Guid tenantId, BuildingId buildingId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken) =>
        await db.Set<ServiceChargeRule>()
            .AsNoTracking()
            .Where(r =>
                r.TenantId == tenantId &&
                r.BuildingId == buildingId &&
                r.IsActive &&
                r.EffectiveFrom <= periodStart &&
                (r.EffectiveTo == null || r.EffectiveTo >= periodEnd))
            .ToListAsync(cancellationToken);

    public async Task<bool> HasOverlappingRuleAsync(
        Guid tenantId,
        BuildingId buildingId,
        string category,
        FlatType? unitTypeFilter,
        BillingFrequency frequency,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        CancellationToken cancellationToken)
    {
        DateOnly newRangeEnd = effectiveTo ?? DateOnly.MaxValue;

        return await db.Set<ServiceChargeRule>()
            .AsNoTracking()
            .Where(r =>
                r.TenantId == tenantId &&
                r.BuildingId == buildingId &&
                r.Category == category &&
                r.UnitTypeFilter == unitTypeFilter &&
                r.Frequency == frequency &&
                r.EffectiveFrom <= newRangeEnd &&
                (r.EffectiveTo == null ? DateOnly.MaxValue : r.EffectiveTo.Value) >= effectiveFrom)
            .AnyAsync(cancellationToken);
    }

    public void Add(ServiceChargeRule rule) => db.Set<ServiceChargeRule>().Add(rule);
}
