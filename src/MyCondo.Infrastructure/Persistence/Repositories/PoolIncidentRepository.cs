using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Amenities.PoolIncidents;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class PoolIncidentRepository(MyCondoDbContext db) : IPoolIncidentRepository
{
    public Task<PoolIncident?> GetByIdAsync(PoolIncidentId id, CancellationToken cancellationToken) =>
        db.Set<PoolIncident>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<PagedResult<PoolIncident>> SearchAsync(
        Guid tenantId, FacilityId? facilityId, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<PoolIncident> query = db.Set<PoolIncident>()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId);

        if (facilityId is not null)
        {
            query = query.Where(p => p.FacilityId == facilityId);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<PoolIncident> items = await query
            .OrderByDescending(p => p.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PoolIncident>(items, page, pageSize, total);
    }

    public void Add(PoolIncident poolIncident) => db.Set<PoolIncident>().Add(poolIncident);
}
