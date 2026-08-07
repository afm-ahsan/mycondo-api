using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Amenities.BlackoutDates;
using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class BlackoutDateRepository(MyCondoDbContext db) : IBlackoutDateRepository
{
    public Task<BlackoutDate?> GetByIdAsync(BlackoutDateId id, CancellationToken cancellationToken) =>
        db.Set<BlackoutDate>().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<BlackoutDate>> GetActiveForFacilityAsync(
        Guid tenantId, FacilityId facilityId, CancellationToken cancellationToken) =>
        await db.Set<BlackoutDate>()
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.FacilityId == facilityId && b.IsActive)
            .OrderBy(b => b.DateFrom)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BlackoutDate>> ListForFacilityAsync(
        Guid tenantId, FacilityId facilityId, CancellationToken cancellationToken) =>
        await db.Set<BlackoutDate>()
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.FacilityId == facilityId)
            .OrderByDescending(b => b.DateFrom)
            .ToListAsync(cancellationToken);

    public void Add(BlackoutDate blackoutDate) => db.Set<BlackoutDate>().Add(blackoutDate);
}
