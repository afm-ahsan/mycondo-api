using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class FacilityRepository(MyCondoDbContext db) : IFacilityRepository
{
    public Task<Facility?> GetByIdAsync(FacilityId id, CancellationToken cancellationToken) =>
        db.Set<Facility>().FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<PagedResult<Facility>> SearchAsync(
        Guid tenantId, BuildingId? buildingId, FacilityType? facilityType, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<Facility> query = db.Set<Facility>()
            .AsNoTracking()
            .Where(f => f.TenantId == tenantId);

        if (buildingId is not null)
        {
            query = query.Where(f => f.BuildingId == buildingId);
        }

        if (facilityType is not null)
        {
            query = query.Where(f => f.FacilityType == facilityType);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<Facility> items = await query
            .OrderBy(f => f.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Facility>(items, page, pageSize, total);
    }

    public async Task LockForCapacityCheckAsync(FacilityId id, CancellationToken cancellationToken) =>
        await db.Database
            .SqlQuery<Guid>($"SELECT id AS \"Value\" FROM amenities.facilities WHERE id = {id.Value} FOR UPDATE")
            .ToListAsync(cancellationToken);

    public void Add(Facility facility) => db.Set<Facility>().Add(facility);
}
