using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class ResidentRepository(MyCondoDbContext db) : IResidentRepository
{
    public Task<Resident?> GetByIdAsync(ResidentId id, CancellationToken cancellationToken) =>
        db.Set<Resident>().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<PagedResult<Resident>> SearchAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<Resident> query = db.Set<Resident>()
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r =>
                EF.Functions.ILike(r.FullName, $"%{search}%") ||
                (r.Phone != null && EF.Functions.ILike(r.Phone, $"%{search}%")) ||
                (r.Email != null && EF.Functions.ILike(r.Email, $"%{search}%")));
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<Resident> items = await query
            .OrderBy(r => r.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Resident>(items, page, pageSize, total);
    }

    public Task<Resident?> FindByFlatAndNameAsync(
        Guid tenantId, FlatId flatId, string fullName, CancellationToken cancellationToken) =>
        db.Set<Resident>().FirstOrDefaultAsync(
            r => r.TenantId == tenantId && r.FlatId == flatId && EF.Functions.ILike(r.FullName, fullName),
            cancellationToken);

    public void Add(Resident resident) => db.Set<Resident>().Add(resident);
}
