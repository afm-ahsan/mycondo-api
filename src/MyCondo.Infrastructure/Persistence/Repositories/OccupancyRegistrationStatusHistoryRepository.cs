using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationStatusHistories;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class OccupancyRegistrationStatusHistoryRepository(MyCondoDbContext db)
    : IOccupancyRegistrationStatusHistoryRepository
{
    public async Task<IReadOnlyList<OccupancyRegistrationStatusHistory>> GetForRegistrationAsync(
        OccupancyRegistrationId occupancyRegistrationId, CancellationToken cancellationToken) =>
        await db.Set<OccupancyRegistrationStatusHistory>()
            .AsNoTracking()
            .Where(x => x.OccupancyRegistrationId == occupancyRegistrationId)
            .OrderBy(x => x.ChangedAtUtc)
            .ToListAsync(cancellationToken);

    public void Add(OccupancyRegistrationStatusHistory entry) => db.Set<OccupancyRegistrationStatusHistory>().Add(entry);
}
