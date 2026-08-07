using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Amenities.PoolSessions;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class PoolSessionRepository(MyCondoDbContext db) : IPoolSessionRepository
{
    public Task<PoolSession?> GetByIdAsync(PoolSessionId id, CancellationToken cancellationToken) =>
        db.Set<PoolSession>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<int> CountOpenAsync(Guid tenantId, FacilityId facilityId, CancellationToken cancellationToken) =>
        db.Set<PoolSession>()
            .AsNoTracking()
            .CountAsync(p => p.TenantId == tenantId && p.FacilityId == facilityId && p.ExitAtUtc == null, cancellationToken);

    public Task<PoolSession?> GetOpenForAccompanimentAsync(
        Guid tenantId, FacilityId facilityId, FlatId flatId, CancellationToken cancellationToken) =>
        db.Set<PoolSession>()
            .FirstOrDefaultAsync(
                p => p.TenantId == tenantId && p.FacilityId == facilityId && p.FlatId == flatId
                    && p.ExitAtUtc == null && p.AgeCategory == PoolAgeCategory.Adult,
                cancellationToken);

    public async Task<PagedResult<PoolSession>> SearchAsync(
        Guid tenantId, FacilityId? facilityId, FlatId? flatId, bool? openOnly, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<PoolSession> query = db.Set<PoolSession>()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId);

        if (facilityId is not null)
        {
            query = query.Where(p => p.FacilityId == facilityId);
        }

        if (flatId is not null)
        {
            query = query.Where(p => p.FlatId == flatId);
        }

        if (openOnly is true)
        {
            query = query.Where(p => p.ExitAtUtc == null);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<PoolSession> items = await query
            .OrderByDescending(p => p.EntryAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PoolSession>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<PoolSession>> GetForDateAsync(
        Guid tenantId, FacilityId facilityId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken) =>
        await db.Set<PoolSession>()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.FacilityId == facilityId && p.EntryAtUtc >= fromUtc && p.EntryAtUtc < toUtc)
            .ToListAsync(cancellationToken);

    public void Add(PoolSession poolSession) => db.Set<PoolSession>().Add(poolSession);
}
