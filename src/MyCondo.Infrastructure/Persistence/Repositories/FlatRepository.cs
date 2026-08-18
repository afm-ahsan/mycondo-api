using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class FlatRepository(MyCondoDbContext db) : IFlatRepository
{
    public Task<Flat?> GetByIdAsync(FlatId id, CancellationToken cancellationToken) =>
        db.Set<Flat>().FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public Task<List<Flat>> GetByIdsAsync(IReadOnlyCollection<FlatId> ids, CancellationToken cancellationToken) =>
        ids.Count == 0
            ? Task.FromResult(new List<Flat>())
            : db.Set<Flat>().AsNoTracking().Where(f => ids.Contains(f.Id)).ToListAsync(cancellationToken);

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

    public async Task<PagedResult<Flat>> SearchForTenantAsync(
        Guid tenantId,
        BuildingId? buildingId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<Flat> query = db.Set<Flat>()
            .AsNoTracking()
            .Where(f => f.TenantId == tenantId);

        if (buildingId is not null)
        {
            query = query.Where(f => f.BuildingId == buildingId);
        }

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

    public async Task<PagedResult<Flat>> SearchMissingAreaSqFtAsync(
        Guid tenantId,
        BuildingId buildingId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<Flat> query = db.Set<Flat>()
            .AsNoTracking()
            .Where(f => f.TenantId == tenantId && f.BuildingId == buildingId && f.AreaSqFt == null);

        long total = await query.LongCountAsync(cancellationToken);

        List<Flat> items = await query
            .OrderBy(f => f.FlatNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Flat>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<Flat>> GetAllForBuildingAsync(
        Guid tenantId, BuildingId buildingId, CancellationToken cancellationToken) =>
        await db.Set<Flat>()
            .AsNoTracking()
            .Where(f => f.TenantId == tenantId && f.BuildingId == buildingId)
            .OrderBy(f => f.FlatNumber)
            .ToListAsync(cancellationToken);

    public void Add(Flat flat) => db.Set<Flat>().Add(flat);
}
