using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Utilities.Meters;
using MyCondo.Domain.Features.Utilities.Readings;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class ReadingRepository(MyCondoDbContext db) : IReadingRepository
{
    private static readonly ReadingStatus[] FinalizedOrLater = [ReadingStatus.Finalized, ReadingStatus.Billed];

    public Task<Reading?> GetByIdAsync(ReadingId id, CancellationToken cancellationToken) =>
        db.Set<Reading>().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<PagedResult<Reading>> SearchAsync(
        Guid tenantId, MeterId? meterId, FlatId? flatId, ReadingStatus? status, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<Reading> query = db.Set<Reading>()
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId);

        if (meterId is not null)
        {
            query = query.Where(r => r.MeterId == meterId);
        }

        if (flatId is not null)
        {
            query = query.Where(r => r.FlatId == flatId);
        }

        if (status is not null)
        {
            query = query.Where(r => r.Status == status);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<Reading> items = await query
            .OrderByDescending(r => r.PeriodEnd)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Reading>(items, page, pageSize, total);
    }

    public Task<Reading?> GetLastFinalizedAsync(Guid tenantId, MeterId meterId, CancellationToken cancellationToken) =>
        db.Set<Reading>()
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.MeterId == meterId && FinalizedOrLater.Contains(r.Status))
            .OrderByDescending(r => r.PeriodEnd)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Reading>> GetRecentFinalizedAsync(
        Guid tenantId, MeterId meterId, int count, CancellationToken cancellationToken) =>
        await db.Set<Reading>()
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.MeterId == meterId && FinalizedOrLater.Contains(r.Status))
            .OrderByDescending(r => r.PeriodEnd)
            .Take(count)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsActiveForMeterAndPeriodAsync(
        Guid tenantId, MeterId meterId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken) =>
        db.Set<Reading>()
            .AsNoTracking()
            .AnyAsync(r =>
                r.TenantId == tenantId && r.MeterId == meterId && r.PeriodStart == periodStart &&
                r.PeriodEnd == periodEnd && r.Status != ReadingStatus.Corrected,
                cancellationToken);

    public async Task<IReadOnlyList<Reading>> GetConsumptionHistoryAsync(
        Guid tenantId, MeterId meterId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken) =>
        await db.Set<Reading>()
            .AsNoTracking()
            .Where(r =>
                r.TenantId == tenantId && r.MeterId == meterId && FinalizedOrLater.Contains(r.Status) &&
                r.PeriodStart >= fromDate && r.PeriodEnd <= toDate)
            .OrderBy(r => r.PeriodStart)
            .ToListAsync(cancellationToken);

    public void Add(Reading reading) => db.Set<Reading>().Add(reading);
}
