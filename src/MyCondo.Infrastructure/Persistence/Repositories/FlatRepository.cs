using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class FlatRepository(MyCondoDbContext db) : IFlatRepository
{
    public Task<Flat?> GetByIdAsync(FlatId id, CancellationToken cancellationToken) =>
        db.Set<Flat>().FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public Task<Flat?> GetByFlatNumberAsync(
        Guid tenantId, BuildingId buildingId, string flatNumber, CancellationToken cancellationToken) =>
        db.Set<Flat>().FirstOrDefaultAsync(
            f => f.TenantId == tenantId && f.BuildingId == buildingId && f.FlatNumber == flatNumber,
            cancellationToken);

    public async Task<PagedResult<Flat>> SearchAsync(
        Guid tenantId,
        BuildingId buildingId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<Flat> query = db.Set<Flat>()
            .AsNoTracking()
            .Where(f => f.TenantId == tenantId && f.BuildingId == buildingId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(f => EF.Functions.ILike(f.FlatNumber, $"%{search}%"));
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<Flat> items = await query
            .OrderBy(f => f.FlatNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Flat>(items, page, pageSize, total);
    }

    public void Add(Flat flat) => db.Set<Flat>().Add(flat);
}
