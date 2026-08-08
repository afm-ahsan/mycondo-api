using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class ParcelRepository(MyCondoDbContext db) : IParcelRepository
{
    public Task<Parcel?> GetByIdAsync(ParcelId id, CancellationToken cancellationToken) =>
        db.Set<Parcel>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<PagedResult<Parcel>> SearchAsync(
        Guid tenantId,
        ParcelStatus? status,
        FlatId? recipientFlatId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<Parcel> query = db.Set<Parcel>()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId);

        if (status is not null)
        {
            query = query.Where(p => p.Status == status);
        }

        if (recipientFlatId is not null)
        {
            query = query.Where(p => p.RecipientFlatId == recipientFlatId);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<Parcel> items = await query
            .OrderByDescending(p => p.ReceivedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Parcel>(items, page, pageSize, total);
    }

    private static readonly ParcelStatus[] UnresolvedStatuses =
        [ParcelStatus.Received, ParcelStatus.ResidentNotified, ParcelStatus.AwaitingCollection];

    public Task<int> GetAwaitingCollectionCountAsync(Guid tenantId, CancellationToken cancellationToken) =>
        db.Set<Parcel>()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && UnresolvedStatuses.Contains(p.Status))
            .CountAsync(cancellationToken);

    public void Add(Parcel parcel) => db.Set<Parcel>().Add(parcel);
}
