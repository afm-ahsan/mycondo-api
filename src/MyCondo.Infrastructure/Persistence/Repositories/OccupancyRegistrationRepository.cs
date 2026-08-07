using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class OccupancyRegistrationRepository(MyCondoDbContext db) : IOccupancyRegistrationRepository
{
    public Task<OccupancyRegistration?> GetByIdAsync(OccupancyRegistrationId id, CancellationToken cancellationToken) =>
        db.Set<OccupancyRegistration>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<PagedResult<OccupancyRegistration>> SearchAsync(
        Guid tenantId, FlatId? flatId, OccupancyRegistrationStatus? status, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<OccupancyRegistration> query = db.Set<OccupancyRegistration>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (flatId is not null)
        {
            query = query.Where(x => x.FlatId == flatId);
        }

        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<OccupancyRegistration> items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<OccupancyRegistration>(items, page, pageSize, total);
    }

    public Task<OccupancyRegistration?> GetActiveForFlatAsync(
        Guid tenantId, FlatId flatId, CancellationToken cancellationToken) =>
        db.Set<OccupancyRegistration>().FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.FlatId == flatId && x.Status == OccupancyRegistrationStatus.Active,
            cancellationToken);

    public void Add(OccupancyRegistration registration) => db.Set<OccupancyRegistration>().Add(registration);
}
