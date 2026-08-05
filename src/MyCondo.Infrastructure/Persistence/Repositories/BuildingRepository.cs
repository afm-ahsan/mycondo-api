using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class BuildingRepository(MyCondoDbContext db) : IBuildingRepository
{
    public Task<Building?> GetByIdAsync(BuildingId id, CancellationToken cancellationToken) =>
        db.Set<Building>().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<Building?> GetByNameAsync(Guid tenantId, string name, CancellationToken cancellationToken) =>
        db.Set<Building>().FirstOrDefaultAsync(b => b.TenantId == tenantId && b.Name == name, cancellationToken);

    public async Task<PagedResult<Building>> SearchAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<Building> query = db.Set<Building>()
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(b => EF.Functions.ILike(b.Name, $"%{search}%"));
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<Building> items = await query
            .OrderBy(b => b.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Building>(items, page, pageSize, total);
    }

    public void Add(Building building) => db.Set<Building>().Add(building);
}
