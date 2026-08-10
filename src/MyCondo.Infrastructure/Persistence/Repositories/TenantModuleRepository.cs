using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class TenantModuleRepository(MyCondoDbContext db) : ITenantModuleRepository
{
    public Task<List<TenantModule>> GetEnabledForTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        db.Set<TenantModule>().Where(m => m.TenantId == tenantId).ToListAsync(cancellationToken);

    public async Task<Dictionary<Guid, int>> GetEnabledCountsAsync(
        IReadOnlyCollection<Guid> tenantIds, CancellationToken cancellationToken)
    {
        if (tenantIds.Count == 0)
        {
            return [];
        }

        return await db.Set<TenantModule>()
            .Where(m => tenantIds.Contains(m.TenantId))
            .GroupBy(m => m.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);
    }

    public async Task ReplaceForTenantAsync(
        Guid tenantId,
        IReadOnlyCollection<string> moduleKeys,
        DateTimeOffset nowUtc,
        Guid? enabledBy,
        CancellationToken cancellationToken)
    {
        List<TenantModule> current = await db.Set<TenantModule>()
            .Where(m => m.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        HashSet<string> requested = new(moduleKeys, StringComparer.Ordinal);
        HashSet<string> existing = new(current.Select(m => m.ModuleKey), StringComparer.Ordinal);

        IEnumerable<TenantModule> toRemove = current.Where(m => !requested.Contains(m.ModuleKey));
        db.Set<TenantModule>().RemoveRange(toRemove);

        foreach (string key in requested.Where(k => !existing.Contains(k)))
        {
            db.Set<TenantModule>().Add(TenantModule.Enable(tenantId, key, nowUtc, enabledBy));
        }
    }
}
