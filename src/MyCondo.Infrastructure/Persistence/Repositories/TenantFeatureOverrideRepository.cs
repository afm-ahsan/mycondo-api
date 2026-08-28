using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class TenantFeatureOverrideRepository(MyCondoDbContext db) : ITenantFeatureOverrideRepository
{
    public Task<List<TenantFeatureOverride>> GetForTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        db.Set<TenantFeatureOverride>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(cancellationToken);

    public async Task<bool> HasOverlappingOverrideAsync(
        Guid tenantId,
        FeatureDefinitionId featureId,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveUntil,
        CancellationToken cancellationToken)
    {
        DateTimeOffset newRangeEnd = effectiveUntil ?? DateTimeOffset.MaxValue;

        return await db.Set<TenantFeatureOverride>()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.FeatureId == featureId &&
                x.EffectiveFrom < newRangeEnd &&
                (x.EffectiveUntil == null ? DateTimeOffset.MaxValue : x.EffectiveUntil.Value) > effectiveFrom)
            .AnyAsync(cancellationToken);
    }

    public void Add(TenantFeatureOverride tenantFeatureOverride) => db.Set<TenantFeatureOverride>().Add(tenantFeatureOverride);
}
