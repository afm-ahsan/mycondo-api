using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Security.ParcelCustodyEvents;
using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class ParcelCustodyEventRepository(MyCondoDbContext db) : IParcelCustodyEventRepository
{
    public Task<List<ParcelCustodyEvent>> GetForParcelAsync(
        Guid tenantId, ParcelId parcelId, CancellationToken cancellationToken) =>
        db.Set<ParcelCustodyEvent>()
            .Where(e => e.TenantId == tenantId && e.ParcelId == parcelId)
            .OrderBy(e => e.OccurredAtUtc)
            .ToListAsync(cancellationToken);

    public void Add(ParcelCustodyEvent custodyEvent) => db.Set<ParcelCustodyEvent>().Add(custodyEvent);
}
