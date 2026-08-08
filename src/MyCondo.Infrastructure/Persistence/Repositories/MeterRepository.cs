using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class MeterRepository(MyCondoDbContext db) : IMeterRepository
{
    public Task<Meter?> GetByIdAsync(MeterId id, CancellationToken cancellationToken) =>
        db.Set<Meter>().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<Meter?> GetByMeterNumberAsync(
        Guid tenantId, UtilityType utilityType, string meterNumber, CancellationToken cancellationToken) =>
        db.Set<Meter>().FirstOrDefaultAsync(
            m => m.TenantId == tenantId && m.UtilityType == utilityType && m.MeterNumber == meterNumber,
            cancellationToken);

    public async Task<PagedResult<Meter>> SearchAsync(
        Guid tenantId, BuildingId buildingId, UtilityType? utilityType, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<Meter> query = db.Set<Meter>()
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.BuildingId == buildingId);

        if (utilityType is not null)
        {
            query = query.Where(m => m.UtilityType == utilityType);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<Meter> items = await query
            .OrderBy(m => m.MeterNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Meter>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<MeterStatusSummaryLine>> GetStatusSummaryAsync(
        Guid tenantId, BuildingId? buildingId, UtilityType? utilityType, CancellationToken cancellationToken)
    {
        IQueryable<Meter> query = db.Set<Meter>().AsNoTracking().Where(m => m.TenantId == tenantId);

        if (buildingId is not null)
        {
            query = query.Where(m => m.BuildingId == buildingId);
        }

        if (utilityType is not null)
        {
            query = query.Where(m => m.UtilityType == utilityType);
        }

        return await query
            .GroupBy(m => new { m.UtilityType, m.Status })
            .Select(g => new MeterStatusSummaryLine(g.Key.UtilityType, g.Key.Status, g.Count()))
            .ToListAsync(cancellationToken);
    }

    public void Add(Meter meter) => db.Set<Meter>().Add(meter);
}
